<script lang="ts">
    import { page } from "$app/state"
    import { goto } from "$app/navigation";
    import { onMount, getContext } from "svelte";
    import { type Readable } from "svelte/store";
    import { config } from "../../config";
    import { type GameMetadataDto, GameState, type PlayerDto, type ActionOptions} from "../../Api";
    import { roleNames, roleDescriptions } from "../../textes/roles";
    import { getPlayerToken, removePlayerToken } from "../../gameSessionStore";
    import { GameHubServer, GameHubClientBase } from "../../signalrHub";
    import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
    import ModalProvider from "../components/ModalProvider.svelte";
    import PageTitle from "../components/PageTitle.svelte";

    const webUrl = page.url.protocol + "//" + page.url.host;     // Port is part of the host
    
    let modalProvider: ModalProvider;
    const modalAccessor = getContext<Readable<ModalProvider>>("modalProvider");
    modalAccessor.subscribe(m => modalProvider = m);
    
    let gameId: number | undefined = $state();
    let selfId: number | undefined = undefined;
    let roleName: string | undefined = $state();
    
    let players: PlayerDto[] = $state([]);
    let metadata: GameMetadataDto | undefined = $state();
    let gameState: GameState | undefined = $state();
    
    let runningAction: ActionOptions | null = $state(null);
    let selectedPlayers: number[] = [];
    
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
            return Promise.resolve(undefined);
        }

        public onPlayerRoleUpdated(newRoleName: string): Promise<void> {
            roleName = newRoleName;
            return Promise.resolve();
        }

        public onActionRequested(action: ActionOptions): Promise<void> {
            selectedPlayers = [];
            runningAction = action;
            return Promise.resolve();
        }

        public onActionCompleted(actionName: string | null, parameters: string[] | null): Promise<void> {
            runningAction = null;
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
{/if}

<div class="flex-grow-1 container-fluid d-flex flex-column align-items-center">
    {#if roleName !== undefined}
        <div class="accordion w-100 mb-3" id="collapseRoleParent">
            <div class="accordion-item">
                <h2 class="accordion-header">
                    <button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#collapseRole" aria-controls="collapseRole">
                        Ihre Rolle
                    </button>
                </h2>
                <div id="collapseRole" class="accordion-collapse collapse" data-bs-parent="#collapseRoleParent">
                    <div class="accordion-body">
                        <h5>{roleNames[roleName]}</h5>
                        {roleDescriptions[roleName]}
                    </div>
                </div>
            </div>
        </div>
    {/if}
    
    <!-- Player display and selection -->
    <ul class="list-group main-content mb-4">
        {#each players as player}
            <li class="list-group-item {getPlayerCSSClasses(player)} d-flex align-items-center">
                {#if runningAction === null}
                    {player.name}
                {:else}
                    {#if runningAction.maximum === 1}
                        <input class="form-check-input me-2" id="playerAction{player.id}" type="radio" value="{player.id}" bind:group={selectedPlayers[0]}
                               name="playerAction" disabled="{runningAction.excludedPlayers?.includes(player.id ?? -1)}" />
                    {:else}
                        <input class="form-check-input me-2" id="playerAction{player.id}" type="checkbox" value="{player.id}" bind:group={selectedPlayers}
                               name="playerAction" disabled="{runningAction.excludedPlayers?.includes(player.id ?? -1)}" />
                    {/if}
                    <label class="form-check-label" for="playerAction{player.id}">{player.name}</label>
                {/if}
                
                {#if selfId === metadata?.gameMasterId}
                    <div class="flex-grow-1"></div>
                    <button type="button" class="btn btn-sm btn-{player.id === selfId ? 'secondary' : 'danger'} w-auto" onclick={() => {
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
        <button class="btn btn-primary main-content" type="button" onclick={() => gameHub.playerAction(selectedPlayers)}>
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