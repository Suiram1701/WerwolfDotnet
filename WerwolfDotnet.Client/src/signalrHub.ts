import { goto } from "$app/navigation";
import { type SelectionOptionsDto, GameState, Role, CauseOfDeath, PlayerRelation, Fraction, type PlayerDto } from "./Api";
import { gamePageState, type GamePageState} from "./stores/pageStateStore";
import { removePlayerToken } from "./stores/gameSessionStore";
import { HubConnection } from "@microsoft/signalr"
import { actionCompletions, actionNames } from "./textes/actions";
import { fractions, fractionWin } from "./textes/fractions";
import { causeOfDeaths } from "./textes/causeOfDeaths";
import { roleNames } from "./textes/roles";
import ModalProvider from "$lib/components/ModalProvider.svelte";

export class GameHub {
    private connection: HubConnection;
    private gamePage!: GamePageState;
    private modal: ModalProvider;

    public constructor(connection: HubConnection, provider: ModalProvider) {
        gamePageState.subscribe(state => this.gamePage = state);
        this.modal = provider;
        
        this.connection = connection;     // Wrapper is required here when subscribing because without it the "this-context" would get lost
        this.connection.on("onGameMetaUpdated", (gmId, mayorId) => this.onGameMetaUpdated(gmId, mayorId));
        this.connection.on("onPlayersUpdated", players => this.onPlayersUpdated(players));
        this.connection.on("onGameStateUpdated", (state, diedPlayers) => this.onGameStateUpdated(state, diedPlayers));
        this.connection.on("onPlayerRoleUpdated", (role, relations) => this.onPlayerRoleUpdated(role, relations));
        this.connection.on("onActionRequested", action => this.onActionRequested(action));
        this.connection.on("onVotesUpdated", votes => this.onVotesUpdated(votes));
        this.connection.on("onActionCompleted", params => this.onActionCompleted(params));
        this.connection.on("onGameWon", fraction => this.onGameWon(fraction));
        this.connection.on("onForceDisconnect", kicked => this.onForceDisconnect(kicked));
    }
    
    public async setGameLocked(locked: boolean): Promise<void> {
        await this.connection.invoke("setGameLocked", locked);
    }
    
    public async shufflePlayers(): Promise<void> {
        await this.connection.invoke("shufflePlayers");
    }
    
    public async startGame(): Promise<void> {
        await this.connection.invoke("startGame");
    }
    
    public async playerAction(selectedPlayer: number[]): Promise<void> {
        await this.connection.invoke("playerAction", selectedPlayer);
    }
    
    public async stopGame(): Promise<void> {
        await this.connection.invoke("stopGame");
    }
    
    public async leaveGame(playerId: number | null = null): Promise<void> {
        await this.connection.invoke("leaveGame", playerId);
    }

    private onGameMetaUpdated(gameMasterId: number, mayorId: number | null): void {
        if (this.gamePage.gameMeta !== undefined && this.gamePage.gameMeta.gameMaster !== gameMasterId && gameMasterId === this.gamePage.selfId)
            this.modal.show({ title: "Game-master wechsel", contentText: "Der aktuelle Game-master hat das Spiel verlassen. Sie sind nun der neue Game-master.", canDismiss: false });
        gamePageState.update(state => {
            state.gameMeta = { gameMaster: gameMasterId, mayor: mayorId };
            return state;
        });
    }

    private onPlayersUpdated(updatedPlayers: PlayerDto[]): void {
        gamePageState.update(state => {
            state.players = updatedPlayers
            return state;
        });
    }

    private onGameStateUpdated(newState: GameState, diedPlayers: Record<number, DeathDetails>): void {
        if (this.gamePage.gameState === GameState.GameWon && newState <= 0) {
            window.location.reload();     // Reset the whole frontend. In my opinion is the risk to break something with a manuell reset.
            return;
        }
        else if (this.gamePage.gameState === undefined || this.gamePage.gameState <= 0) {     // Frontend has just initialized. No one can be displayed as died during this
            gamePageState.update(state => {
                state.gameState = newState;
                return state;
            });
            return;
        }

        gamePageState.update(state => {
            state.gameState = newState;
            for (const player of this.gamePage.players) {
                if (player.id! in diedPlayers)
                    player.alive = false;
            }
            return state;
        });

        if (this.gamePage.selfId! in diedPlayers)
        {
            this.modal.show({ title: "Du bist gestorben", contentText: "Du bist gestorben. Ab sofort kannst du dem Spiel nur noch zuschauen.", canDismiss: false });
            return;
        }

        const diedStr: string = this.buildDeathString(diedPlayers);
        if (newState === GameState.Day)
            this.modal.show({ title: "Der Tag bricht an", contentText: `Der Tag bricht an. ${diedStr}`, allowHtmlText: true, canDismiss: false });
        else if (newState === GameState.Night)
            this.modal.show({ title: "Die Nacht bricht an", contentText: `Die Nacht beginnt. ${diedStr}`, allowHtmlText: true, canDismiss: false });
    }

    private onPlayerRoleUpdated(newRoleName: Role, relations: Record<number, PlayerRelation[]>): void {
        gamePageState.update(state => {
            state.selfRole = newRoleName;
            state.playerRelations = relations;
            return state;
        });
    }

    private onActionRequested(action: SelectionOptionsDto): void {
        gamePageState.update(state => {
            state.selectedPlayers = [];
            state.currentAction = action;
            return state;
        })
    }

    private onVotesUpdated(votes: Record<number, number[]>): void {
        gamePageState.update(state => {
            state.selectedPlayers = state.selfId! in votes ? votes[state.selfId!] : [];

            const votesForPlayer: Record<number, number[]> = {};
            for (const by in votes) {
                for (const votedOne of votes[by]) {
                    if (votedOne in votesForPlayer)
                        votesForPlayer[votedOne].push(Number(by));
                    else
                        votesForPlayer[votedOne] = [Number(by)];
                }
            }
            state.playerToVotes = votesForPlayer;
            state.emptyVotedPlayers = Object.entries(votes).filter(t => t[1].length === 0).map(t => Number(t[0]))
            
            return state;
        });
    }

    private onActionCompleted(parameters: string[] | null): void {
        if (parameters !== null) {
            this.modal.show({
                title: actionNames[this.gamePage.currentAction!.type ?? 0] || "undefined",
                contentText: actionCompletions[this.gamePage.currentAction!.type ?? 0]?.(parameters) || "Wenn du dies siehst ist etwas schiefgelaufen...",
                allowHtmlText: true,
                canDismiss: false
            });
        }

        gamePageState.update(state => {
            state.currentAction = null;
            state.playerToVotes = [];
            state.emptyVotedPlayers = [];
            return state;
        });
    }

    private onGameWon(winFraction: Fraction): void {
        gamePageState.update(state => {
            state.gameState = GameState.GameWon;
            return state;
        });
        this.modal.show({
            title: `Die ${fractions[winFraction]}`,
            contentText: `Die ${fractions[winFraction]} haben gewonnen. ${fractionWin[winFraction]}`
        });
    }

    private async onForceDisconnect(kicked: boolean): Promise<void> {
        await this.connection.stop();
        removePlayerToken(this.gamePage.gameId, this.gamePage.selfId);
        
        if (kicked)
            goto("/?kicked=true");
        else
            goto("/");
    }

    private buildDeathString(diedPlayers: Record<number, DeathDetails>): string {
        if (Object.keys(diedPlayers).length > 0)
        {
            let diedStr: string = "";
            let mapped: Partial<Record<CauseOfDeath, number[]>> = {};
            for (const player in diedPlayers) {
                const cause = diedPlayers[player].cause;
                if (cause in mapped)
                    mapped[cause]!.push(Number(player));
                else
                    mapped[cause] = [Number(player)];
            }

            for (const cause of Object.keys(CauseOfDeath).map(k => Number(k) as CauseOfDeath))
            {
                const diedFromCause: number[] = mapped[cause] ?? [];
                if (diedFromCause.length > 0)
                    diedStr += `${causeOfDeaths[cause](mapped[cause]!.map(id => this.gamePage.players.find(p => p.id! === id)!.name!) ?? [])} `;

                if (diedFromCause.length === 1 && diedPlayers[diedFromCause[0]]?.role !== Role.None)
                {
                    diedStr += `Er war <b>${roleNames[diedPlayers[diedFromCause[0]].role]}</b>. `;
                }
                else if (diedFromCause.filter(id => diedPlayers[id].role !== Role.None).length > 1)
                {
                    diedStr += diedFromCause
                        .filter(id => diedPlayers[id].role !== Role.None)
                        .map(playerId => `<b>${this.gamePage.players.find(p => p.id === playerId)!.name}</b> war <b>${roleNames[diedPlayers[playerId].role]}</b>`)
                        .join(", ");
                    diedStr += ". ";
                }
            }
            return diedStr;
        }

        return "Es ist niemand gestorben.";
    }
}

export type DeathDetails = { cause: CauseOfDeath, role: Role };