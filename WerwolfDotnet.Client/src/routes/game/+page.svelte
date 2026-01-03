<script lang="ts">
    import { page } from "$app/state"
    import { goto } from "$app/navigation";
    import { onMount, getContext } from "svelte";
    import { type Readable } from "svelte/store";
    import { config } from "../../config";
    import { type PlayerDto } from "../../Api";
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
    let playerId: number | undefined = undefined;
    let players: PlayerDto[] = $state([]);
    
    let connection: HubConnection;
    let gameHub: GameHubServer;
    let gameHubClient: GameHubClientBase;
    onMount(() => {
        gameId = Number.parseInt(page.url.searchParams.get("sessionId") ?? "");
        playerId = Number.parseInt(page.url.searchParams.get("playerId") ?? "");
        
        const playerToken = getPlayerToken(gameId, playerId);
        if (playerToken === undefined) {
            goto("/");
            return;
        }
        
        connection = new HubConnectionBuilder()
            .withUrl(`${config.apiEndpoint}/signalr/game?sessionId=${gameId}&playerId=${playerId}`, {
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
        
        onPlayersUpdated(updatedPlayers: PlayerDto[]): Promise<void> {
            players = updatedPlayers;
            return Promise.resolve();
        }
        
        async onForceDisconnect(kicked: boolean): Promise<void> {
            await connection.stop();
            removePlayerToken(gameId ?? -1, playerId ?? -1);
            if (kicked)
                goto("/?kicked=true");
            else
                goto("/");
        }
    }
    
    function getPlayerCSSClasses(player: PlayerDto): string {
        if (player.id === playerId) 
            return "list-group-item-success";
        return "";
    }
</script>

{#snippet leaveGame()}Möchten Sie das Spiel wirklich verlassen?{/snippet}
{#snippet kickPlayer()}Möchten Sie diesen Spieler wirklich aus dem Spiel werden?{/snippet}

<PageTitle title="Werwolf - Spiel {gameId}" />

<div class="mb-4">
    <p>Andere Spieler können beitreten indem Sie diese Website (<a href="{webUrl}">{page.url.host}</a>) gehen und den Spielcode <b>{gameId?.toString().padStart(6, '0')}</b> eingeben.</p>
    <p>Direktes beitreten ist auch über <a href="{webUrl}?gameId={gameId}">diesen Link</a> möglich.</p>
</div>

<div class="main-content container-fluid d-flex flex-column align-items-center">
    <ul class="list-group">
        {#each players as player}
            <li class="list-group-item {getPlayerCSSClasses(player)} d-flex justify-content-between align-items-center">
                {player.name}
                <button type="button" class="w-auto btn btn-sm btn-{player.id === playerId ? 'secondary' : 'danger'}" onclick={() => {
                    if (player.id === playerId)
                        return;
                    modalProvider.show("Spiel Kicken?", leaveGame, true, "Kicken", "danger", () => gameHub.leaveGame(player.id));
                }} disabled="{player.id === playerId}">Kicken</button>
            </li>
        {/each}
    </ul>
    
    <button class="btn btn-outline-danger mt-3" type="button" onclick={() => {
         modalProvider.show("Spiel verlassen?", kickPlayer, true, "Kicken", "danger", () => gameHub.leaveGame());
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