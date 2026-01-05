<script lang="ts">
    import { page } from "$app/state"
    import { goto } from "$app/navigation";
    import { onMount, getContext } from "svelte";
    import { type Readable } from "svelte/store";
    import { config } from "../../config";
    import { type GameMetadataDto, type GameStateDto, GameState, type PlayerDto } from "../../Api";
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
    
    let players: PlayerDto[] = $state([]);
    let metadata: GameMetadataDto | undefined = $state();
    let gameStatus: GameStateDto | undefined = $state();
    
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
                modalProvider.showSimple("Game-master wechsel", "Der aktuelle Game-master hat das Spiel verlassen. Sie sind nun der neue Game-master.")
            metadata = meta;
            return Promise.resolve();
        }
        
        onPlayersUpdated(updatedPlayers: PlayerDto[]): Promise<void> {
            players = updatedPlayers;
            return Promise.resolve();
        }
        
        onGameStateUpdated(gameState: GameStateDto): Promise<void> {
            gameStatus = gameState;
            return Promise.resolve(undefined);
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
        return "";
    }
</script>

{#snippet leaveGame()}Möchten Sie das Spiel wirklich verlassen?{/snippet}
{#snippet kickPlayer()}Möchten Sie diesen Spieler wirklich aus dem Spiel werden?{/snippet}

<PageTitle title="Werwolf - Spiel {gameId}" />

{#if gameStatus?.currentState === GameState.Preparation}
    <div class="text-center mb-4">
        <p>Andere Spieler können beitreten indem Sie diese Website (<a href="{webUrl}">{page.url.host}</a>) gehen und den Spielcode <b>{gameId?.toString().padStart(6, '0')}</b> eingeben.</p>
        <p>Direktes beitreten ist auch über <a href="{webUrl}?gameId={gameId}">diesen Link</a> möglich.</p>
    </div>
{/if}

<div class="flex-grow-1 container-fluid d-flex flex-column align-items-center main-content">
    <ul class="list-group flex-grow-1 mb-4">
        {#each players as player}
            <li class="list-group-item {getPlayerCSSClasses(player)} d-flex justify-content-between align-items-center">
                {player.name}
                
                {#if selfId === metadata?.gameMasterId}
                    <button type="button" class="w-auto btn btn-sm btn-{player.id === selfId ? 'secondary' : 'danger'}" onclick={() => {
                        if (player.id === selfId)
                            return;
                        modalProvider.show("Spiel Kicken?", kickPlayer, true, "Kicken", "danger", () => {
                            gameHub.leaveGame(player.id)
                            modalProvider.hide();
                        });
                    }} disabled="{player.id === selfId}">Kicken</button>
                {/if}
            </li>
        {/each}
    </ul>

    {#if selfId === metadata?.gameMasterId && (gameStatus?.currentState ?? -2) <= GameState.Locked}
        <div class="d-flex mb-3">
            <button class="btn btn-primary" type="button" onclick={() => {
                // TODO: Not implemented yet
            }}>Spiel starten</button>

            <button class="btn btn-info mx-2" type="button" onclick={() => {
                // TODO: Not implemented yet
            }}>Spieler durchmischen</button>

            {#if gameStatus?.currentState !== GameState.Locked}
                <button class="btn btn-warning" type="button" onclick={() => gameHub.toggleGameLocked()}>Beitreten blockieren</button>
            {:else}
                <button class="btn btn-success" type="button" onclick={() => gameHub.toggleGameLocked()}>Beitreten erlauben</button>
            {/if}
        </div>
    {/if}
    
    <button class="btn btn-danger" type="button" onclick={() => {
         modalProvider.show("Spiel verlassen?", leaveGame, true, "Verlassen", "danger", () => gameHub.leaveGame());
    }}>Spiel verlassen</button>
</div>

<style>
    @media (max-width: 576px) {
        .main-content * {
            width: 100%;
            max-width: 100vw;
        }
    }

    @media (min-width: 576px) and (max-width: 1200px) {
        .main-content * {
            width: 100%;
            max-width: 35rem;
        }
    }

    @media (min-width: 1200px) {
        .main-content * {
            width: 100%;
            max-width: 45rem;
        }
    }
</style>