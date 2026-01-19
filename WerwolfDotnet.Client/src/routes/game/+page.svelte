<script lang="ts">
    import { page } from "$app/state"
    import { goto } from "$app/navigation";
    import { onMount, getContext } from "svelte";
    import { type Readable } from "svelte/store";
    import { config } from "../../config";
    import { type GameMetadataDto, GameState, Role, type PlayerDto, type SelectionOptionsDto } from "../../Api";
    import { roleNames, roleDescriptions } from "../../textes/roles";
    import {actionNames, actionDescriptions, actionCompletions} from "../../textes/actions";
    import { getPlayerToken, removePlayerToken } from "../../gameSessionStore";
    import { GameHubServer, GameHubClientBase } from "../../signalrHub";
    import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
    import ModalProvider from "../components/ModalProvider.svelte";
    import PageTitle from "../components/PageTitle.svelte";
    import { tooltip } from "$lib/actions/tooltip";

    const webUrl = page.url.protocol + "//" + page.url.host;     // Port is part of the host
    
    let modalProvider: ModalProvider;
    const modalAccessor = getContext<Readable<ModalProvider>>("modalProvider");
    modalAccessor.subscribe(m => modalProvider = m);
    
    let gameId: number | undefined = $state();
    let selfId: number | undefined = undefined;
    let selfRole: Role | undefined = $state();
    
    let players: PlayerDto[] = $state([]);
    let metadata: GameMetadataDto | undefined = $state();
    let gameState: GameState | undefined = $state();
    
    let runningAction: SelectionOptionsDto | null = $state(null);
    let selectedPlayers: number[] = $state([]);
    let currentVotes: Record<number, number[]> = $state([]);
    
    let connection: HubConnection;
    let gameHub: GameHubServer;
    let gameHubClient: GameHubClientBase;
    onMount(() => {
        gameId = Number.parseInt(page.url.searchParams.get("sessionId") ?? "");
        selfId = Number.parseInt(page.url.searchParams.get("playerId") ?? "");
        
        const playerToken = getPlayerToken(gameId, selfId);
        if (playerToken === undefined) {
            goto("/");
            return;
        }
        
        connection = new HubConnectionBuilder()
            .withUrl(`${config.apiEndpoint}/signalr/game?sessionId=${gameId}&playerId=${selfId}`, {
                accessTokenFactory: () => playerToken
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();
        connection.start()
            .then(() => {
                gameHub = new GameHubServer(connection);
                gameHubClient = new GameHubClient(connection);
            })
            .catch(err => {
                console.log("An error occurred while trying to connect to the game API!", err)
                goto("/");
            });
        
        return () => connection.stop();
    });
    
    class GameHubClient extends GameHubClientBase {
        constructor(connection: HubConnection) { super(connection); }

        onGameMetaUpdated(meta: GameMetadataDto): Promise<void> {
            if (metadata !== undefined && meta.gameMasterId === selfId)
                modalProvider.show({ title: "Game-master wechsel", contentText: "Der aktuelle Game-master hat das Spiel verlassen. Sie sind nun der neue Game-master." });
            metadata = meta;
            return Promise.resolve();
        }
        
        onPlayersUpdated(updatedPlayers: PlayerDto[]): Promise<void> {
            players = updatedPlayers;
            return Promise.resolve();
        }
        
        onGameStateUpdated(newState: GameState, diedPlayers: number[]): Promise<void> {
            gameState = newState;
            for (const player of players) {
                if (diedPlayers.includes(player.id!))
                    player.alive = false;
            }
            
            if (diedPlayers.includes(selfId!))
                modalProvider.show({ title: "Du bist gestorben", contentText: "Du bist gestorben. Ab sofort kannst du dem Spiel nur noch zuschauen." });
            return Promise.resolve(undefined);
        }

        public onPlayerRoleUpdated(newRoleName: Role): Promise<void> {
            selfRole = newRoleName;
            return Promise.resolve();
        }

        public onActionRequested(action: SelectionOptionsDto): Promise<void> {
            selectedPlayers = [];
            runningAction = action;
            return Promise.resolve();
        }
        
        public onVotesUpdated(votes: Record<number, number[]>): Promise<void> {
            currentVotes = votes;
            
            let ownVotes: number[] = [];
            for (const key in votes) {
                if (votes[key].includes(selfId!))
                    ownVotes.push(Number(key));
            }
            selectedPlayers = ownVotes;
            
            return Promise.resolve();
        }

        public onActionCompleted(parameters: string[] | null): Promise<void> {
            if (parameters !== null) {
                modalProvider.show({
                    title: actionNames[runningAction!.type ?? 0],
                    contentText: actionCompletions[runningAction!.type ?? 0](parameters)
                });
            }
            
            runningAction = null;
            currentVotes = [];
            return Promise.resolve();
        }
        
        async onForceDisconnect(kicked: boolean): Promise<void> {
            await connection.stop();
            removePlayerToken(gameId ?? -1, selfId ?? -1);
            if (kicked)
                goto("/?kicked=true");
            else
                goto("/");
        }
    }
    
    function getPlayerCSSClasses(player: PlayerDto): string {
        if (player.id === selfId) 
            return "list-group-item-success";
        else if (!player.alive)
            return "list-group-item-secondary";
        return "";
    }
</script>

<PageTitle title="Werwolf - Spiel {gameId}" />

{#if gameState === GameState.Preparation}
    <div class="text-center mb-4">
        <p>Andere Spieler können beitreten indem Sie diese Website (<a href="{webUrl}">{page.url.host}</a>) gehen und den Spielcode <b>{gameId?.toString().padStart(6, '0')}</b> eingeben.</p>
        <p>Direktes beitreten ist auch über <a href="{webUrl}?gameId={gameId}">diesen Link</a> möglich.</p>
    </div>
{:else if selfRole !== undefined}
    <div class="accordion w-100 mb-3" id="collapseRoleParent">
        <div class="accordion-item">
            <h2 class="accordion-header">
                <button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#collapseRole" aria-controls="collapseRole">
                    Ihre Rolle
                </button>
            </h2>
            <div id="collapseRole" class="accordion-collapse collapse" data-bs-parent="#collapseRoleParent">
                <div class="accordion-body">
                    <h5>{roleNames[selfRole]}</h5>
                    {roleDescriptions[selfRole]}
                </div>
            </div>
        </div>
    </div>
{/if}

{#if runningAction !== null}
    <div class="text-center mb-3">
        <h5>{actionNames[runningAction.type ?? 0]}</h5>
        <p>{actionDescriptions[runningAction.type ?? 0]}</p>
    </div>
{/if}

<div class="flex-grow-1 container-fluid d-flex flex-column align-items-center">
    <!-- Player display and selection -->
    <ul class="list-group main-content mb-4">
        {#each players as player}
            <li class="list-group-item {getPlayerCSSClasses(player)} d-flex align-items-center">
                {#if runningAction === null}
                    {player.name}
                {:else}
                    <!-- Decide between radio (one selection) and checkbox (multiple selections) -->
                    {#if runningAction.maximum === 1}
                        <input class="form-check-input me-2" id="playerAction{player.id}" type="radio" value="{player.id}" bind:group={selectedPlayers[0]}
                               name="playerAction" disabled="{runningAction.excludedPlayers?.includes(player.id ?? -1) || !player.alive}" />
                    {:else}
                        <input class="form-check-input me-2" id="playerAction{player.id}" type="checkbox" value="{player.id}" bind:group={selectedPlayers}
                               name="playerAction" disabled="{runningAction.excludedPlayers?.includes(player.id ?? -1) || !player.alive}" />
                    {/if}
                    <label class="form-check-label" for="playerAction{player.id}">{player.name}</label>
                {/if}

                <div class="flex-grow-1"></div>

                {#if (player.id ?? 0) in currentVotes && currentVotes[player.id ?? 0].length > 0}
                    <span class="badge text-bg-secondary" use:tooltip={{
                        title: currentVotes[player.id ?? 0].map(id => players.find(p => (p.id ?? 0) === id)?.name).join(', '),
                        placement: "top"
                    }}>
                        {currentVotes[player.id ?? 0].length}
                    </span>
                {/if}
                
                {#if selfId === metadata?.gameMasterId}
                    <button type="button" class="btn btn-sm btn-{player.id === selfId ? 'secondary' : 'danger'} w-auto ms-3" onclick={() => {
                        if (player.id === selfId)
                            return;
                        modalProvider.show({
                            title: "Spieler kicken?",
                            contentText: "Möchten Sie diesen Spieler wirklich aus dem Spiel werden?",
                            confirmText: "Kicken",
                            confirmColor: "danger",
                            onConfirm: () => gameHub.leaveGame(player.id),
                            closeOnConfirm: true
                        });
                    }} disabled="{player.id === selfId}">Kicken</button>
                {/if}
            </li>
        {/each}
    </ul>

    {#if runningAction !== null}
        <button class="btn btn-primary main-content" type="button" onclick={() => gameHub.playerAction(selectedPlayers)}
                disabled="{selectedPlayers.length < (runningAction.minimum ?? 0) || selectedPlayers.length > (runningAction.maximum ?? 0)}">
            Abschicken
        </button>
    {/if}

    <div class="flex-grow-1"></div>
    
    <!-- Admin buttons -->
    {#if selfId === metadata?.gameMasterId && (gameState ?? -2) <= GameState.Locked}
        <div class="d-flex main-content mb-3">
            <button class="btn btn-primary w-100" type="button" onclick={() => gameHub.startGame()} disabled="{players.length < 3}">Spiel starten</button>
            <button class="btn btn-info w-100 mx-2" type="button" onclick={() => gameHub.shufflePlayers()}>Spieler durchmischen</button>

            {#if gameState !== GameState.Locked}
                <button class="btn btn-warning w-100" type="button" onclick={() => gameHub.toggleGameLocked()}>Beitreten blockieren</button>
            {:else}
                <button class="btn btn-success w-100" type="button" onclick={() => gameHub.toggleGameLocked()}>Beitreten erlauben</button>
            {/if}
        </div>
    {/if}
    
    <!-- Leave button -->
    <button class="btn btn-danger main-content" type="button" onclick={() => {
        modalProvider.show({
            title: "Spiel verlassen?",
            contentText: "Möchten Sie das Spiel wirklich verlassen?",
            confirmText: "Verlassen",
            confirmColor: "danger",
            onConfirm: () => {
                gameHub.leaveGame();
                goto("/");
            }
        });
    }}>Spiel verlassen</button>
</div>

<style>
    @media (max-width: 576px) {
        .main-content {
            width: 100%;
            max-width: 100vw;
        }
    }

    @media (min-width: 576px) and (max-width: 1200px) {
        .main-content {
            width: 100%;
            max-width: 35rem;
        }
    }

    @media (min-width: 1200px) {
        .main-content {
            width: 100%;
            max-width: 45rem;
        }
    }
</style>