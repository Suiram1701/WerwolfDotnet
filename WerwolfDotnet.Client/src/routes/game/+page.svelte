<script lang="ts">
    import { getContext, onMount } from "svelte";
    import { page } from "$app/state"
    import { goto } from "$app/navigation";
    import { type Readable } from "svelte/store";
    import { ActionType, Api, GameState } from "../../Api";
    import { storePlayerToken, getPlayerToken } from "../../stores/gameSessionStore";
    import { gamePageState as game } from "../../stores/pageStateStore";
    import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
    import { GameHub } from "../../gameHub";
    import { roleDescriptions, roleNames } from "../../textes/roles";
    import { actionDescriptions, actionNames } from "../../textes/actions";
    import { renderMessage } from "../../textes/logs";
    import { tooltip } from "$lib/actions/tooltip";
    import ModalProvider from "$lib/components/ModalProvider.svelte";
    import PageTitle from "$lib/components/PageTitle.svelte";
    import PlayerList from "$lib/components/PlayerList.svelte";
    import GameSettings from "$lib/components/GameSettings.svelte";
    import { default as routes } from "../routes";
    import { config, getServerConfig } from "../../config";

    const serverConfig = getServerConfig();
    const webUrl = page.url.protocol + "//" + page.url.host;     // Port is part of the host
    
    let modalProvider: ModalProvider;
    const modalAccessor = getContext<Readable<ModalProvider>>("modalProvider");
    modalAccessor.subscribe(m => modalProvider = m);

    const apiClient = new Api({ baseUrl: config.apiEndpoint, securityWorker: (data: unknown) => {
        // @ts-ignore
        return { headers: { "Authorization": `Bearer ${data.token}` } };
    }});

    let showAllPlayers = $state(false);
    
    let enoughPlayers = $derived($game.players.length >= (serverConfig.minimumPlayers ?? 0));
    let everyOneReady = $derived(serverConfig.canStartWhenNotReady || $game.playersReady.length === $game.players.length);

    let isWerwolfKilling = $derived($game.currentAction?.type === ActionType.WerwolfKilling);
    let canEditSettings = $derived($game.gameMeta?.gameMaster === $game.selfId && ($game.gameState ?? 0) <= 0);
    
    let connection: HubConnection;
    let gameHub: GameHub;
    onMount(() => {
        const gameId = Number.parseInt(page.url.searchParams.get("sessionId") ?? "-1");
        let selfId: number | undefined;
        let playerToken: string | undefined = undefined;
        
        if (config.allowSessionSharing && page.url.hash.startsWith("#auth=")) {
            selfId = Number.parseInt(page.url.searchParams.get("playerId") ?? "-1");
            playerToken = page.url.hash.substring(6);
            
            storePlayerToken(gameId, selfId, playerToken);
            goto(routes.game(gameId, selfId));     // Remove auth secret from URL
        }
        else {
            selfId = config.allowMultipleSessions ? Number.parseInt(page.url.searchParams.get("playerId") ?? "-1") : undefined;
            const gameSession = getPlayerToken(gameId, selfId);
            playerToken = gameSession?.playerToken;
            selfId = gameSession?.playerId;
        }
        
        if (playerToken === undefined) {
            if ($game.gameId > 0)
                goto(routes.menuJoin(gameId));
            else
                goto(routes.menu());
            return;
        }
        
        game.update(s => {
            s.gameId = gameId;
            s.selfId = selfId!;
            return s;
        });
        
        apiClient.setSecurityData({ token: playerToken });
        connection = new HubConnectionBuilder()
            .withUrl(`${config.apiEndpoint}/signalr/game`, { accessTokenFactory: () => playerToken })
            .withStatefulReconnect()
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();
        connection.onreconnecting(() => document.getElementById('client-disconnected')!.classList.remove('d-none'));
        connection.onreconnected(() => document.getElementById('client-disconnected')!.classList.add('d-none'));
        
        connection.start()
            .then(() => gameHub = new GameHub(connection, modalProvider))
            .catch(err => {
                console.log("An error occurred while trying to connect to the game API!", err)
                goto(routes.menu());
            });
        
        return () => connection.stop();
    });
</script>

{#snippet gameOptionsModalContent()}
    <GameSettings readonly={!canEditSettings} apiClient={apiClient} />
{/snippet}

{#snippet gameLogsModalContent()}
    <div class="overflow-auto">
        {#each $game.gameLogs as message}
            <p>{renderMessage(message)}</p>
        {/each}
    </div>
{/snippet}

<PageTitle title="Werwolf - Spiel {$game.gameId}" />

{#if $game.selfRole !== undefined}
    <div class="accordion w-100 mb-4" id="collapseRoleParent">
        <div class="accordion-item">
            <h2 class="accordion-header">
                <button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#collapseRole" aria-expanded="true" aria-controls="collapseRole">
                    Ihre Rolle
                </button>
            </h2>
            <div id="collapseRole" class="accordion-collapse show" data-bs-parent="#collapseRoleParent">
                <div class="accordion-body">
                    <h5>{roleNames[$game.selfRole]}</h5>
                    {roleDescriptions[$game.selfRole]}
                </div>
            </div>
        </div>
    </div>
{/if}

<div class="text-center mb-4">
    {#if $game.currentAction !== null}
        <h5>{actionNames[$game.currentAction.type ?? 0] || "undefined"}</h5>
        <p>{actionDescriptions[$game.currentAction.type ?? 0] || "Dies solltest du eigentlich nicht sehen :)"}</p>
    {:else if $game.gameState === GameState.Preparation}
        <p>Andere Spieler können beitreten, indem sie auf die Website (<a href="{webUrl}">{page.url.host}</a>) gehen und den Spielcode <b>{$game.gameId?.toString().padStart(6, '0')}</b> eingeben.</p>
        <p>Direktes Beitreten ist auch über <a href="{webUrl}{routes.menuJoin($game.gameId)}">diesen Link</a> möglich.</p>
    {:else if $game.gameState === GameState.Day}
        <p>Der Tag ist angebrochen. Diskutiert und entscheidet euch für einen Spieler, der am Abend hingerichtet werden soll.</p>
    {:else if $game.gameState === GameState.Night}
        <p>Die Nacht ist angebrochen! Das Dorf geht schlafen.</p>
    {:else if $game.gameState === GameState.GameWon}
        <p>
            Die Spielrunde ist zu ende. Warte darauf, dass der Game-Master die aktuelle Runde beendet und eine neue startet.<br>
            <small>Falls Bugs, Glitches oder andere Fehler aufgetreten sind können diese gerne per <a href="https://github.com/Suiram1701/WerwolfDotnet/issues" target="_blank">GitHub</a> gemeldet werden.</small>
        </p>
    {/if}
</div>

<div class="flex-grow-1 container-fluid d-flex flex-column align-items-center">
    <!-- Player display and selection -->
    <ul class="list-group main-content mb-4">
        <PlayerList showAll={showAllPlayers} apiClient={apiClient} />
        
        {#if $game.currentAction !== null && $game.currentAction.minimum === 0}
            <li class="list-group-item d-flex align-items-center d-flex align-items-center">
                {#if $game.currentAction.maximum === 1}
                    <input class="form-check-input me-2" id="playerAction-NoOne" name="playerAction" type="radio"
                           checked="{$game.selectedPlayers.length === 0}" onclick={() => game.update(s => {
                               s.selectedPlayers = [];
                               return s;
                           })}>
                {/if}
                <label class="form-check-label" for="playerAction-NoOne">Niemand auswählen</label>

                <div class="flex-grow-1"></div>
                {#if $game.emptyVotedPlayers.length > 0}
                    <span class="badge text-bg-secondary" use:tooltip={{
                        title: $game.emptyVotedPlayers.map(id => {
                            const name = $game.players.find(p => p.id === id)?.name;
                            return $game.gameMeta?.mayor === id && isWerwolfKilling ? `2x ${name}` : name;
                        }).join(', '),
                        placement: "top"
                    }}>
                        {#if $game.emptyVotedPlayers.includes($game.gameMeta?.mayor ?? -1) && isWerwolfKilling}     <!-- Include the mayor vote -->
                            {$game.emptyVotedPlayers.length + 1}
                        {:else}
                            {$game.emptyVotedPlayers.length}
                        {/if}
                    </span>
                {/if}
            </li>
        {/if}
    </ul>

    {#if $game.currentAction !== null}
        <button class="btn btn-secondary main-content mb-2" type="button" onclick={() => showAllPlayers = !showAllPlayers}>
            {showAllPlayers ? "Nur wählbare Spieler anzeigen" : "Alle anzeigen"}
        </button>
        
        <button class="btn btn-primary main-content" type="button" onclick={() => gameHub.playerAction($game.selectedPlayers)}
                disabled="{$game.selectedPlayers.length < ($game.currentAction.minimum ?? 0) || $game.selectedPlayers.length > ($game.currentAction.maximum ?? 0)}">
            Abschicken
        </button>
    {/if}

    {#if ($game.gameState ?? 0) <= 0}
        {#if $game.playersReady.includes($game.selfId)}
            <button class="btn btn-secondary main-content" type="button" onclick={() => gameHub.setPlayerReady(false)}>Nicht mehr bereit</button>
        {:else}
            <button class="btn btn-primary main-content" type="button" onclick={() => gameHub.setPlayerReady(true)}>Bereit</button>
        {/if}
    {/if}

    <div class="flex-grow-1"></div>

    <div class="small-vp d-flex main-content mt-3">
        <button class="btn btn-secondary d-flex align-items-center justify-content-center w-100" type="button" onclick={() => {
            modalProvider.show({
                title: canEditSettings ? "Spieleinstellungen" : "Spieleinstellungen (nur ansehen)",
                content: gameOptionsModalContent,
                confirmText: "Schließen",
                canDismiss: false,
                closeOnConfirm: true
            });
        }}>
            <span class="material-symbols-outlined me-2">settings</span>
            {canEditSettings ? "Spieleinstellungen" : "Spieleinstellungen ansehen"}
        </button>

        <button class="btn btn-secondary d-flex align-items-center justify-content-center w-100 ms-2" type="button" onclick={() => modalProvider.show({
            title: "Spiel-Log",
            content: gameLogsModalContent,
            confirmText: "Schließen",
            canDismiss: false,
            closeOnConfirm: true
        })}>
            <span class="material-symbols-outlined me-2">manage_search</span>
            Spiel-Logs ansehen
        </button>
    </div>
    
    <!-- Admin buttons -->
    {#if $game.selfId === $game.gameMeta?.gameMaster && ($game.gameState ?? -2) <= 0}
        <div class="d-flex main-content mt-3">
            {#if enoughPlayers && everyOneReady}
                <button class="btn btn-primary w-100" type="button" onclick={async () => await gameHub.startGame()}>Spiel starten</button>
            {:else}
                <span class="d-inline-block w-100" use:tooltip={{ title: !enoughPlayers
                    ? `Es müssen mindestens ${serverConfig.minimumPlayers} in einer Runde sein.`
                    : "Alle Spieler müssen bereit sein (Spieler, die AFK sind können auch gekicked werden)." }}>
                    <button class="btn btn-primary w-100" type="button" disabled>Spiel starten</button>
                </span>
            {/if}
            
            <button class="btn btn-info w-100 mx-2" type="button" onclick={ async () => await gameHub.shufflePlayers()}>Spieler durchmischen</button>

            {#if $game.gameState !== GameState.Locked}
                <button class="btn btn-warning w-100" type="button" onclick={ async () => await gameHub.setGameLocked(true)}>Beitreten blockieren</button>
            {:else}
                <button class="btn btn-success w-100" type="button" onclick={ async () => await gameHub.setGameLocked(false)}>Beitreten erlauben</button>
            {/if}
        </div>
    {/if}
    
    <div class="d-flex main-content m-3">
        <!-- Leave button -->
        <button class="btn btn-danger w-100" type="button" onclick={() => {
            modalProvider.show({
                title: "Spiel verlassen?",
                contentText: "Möchten Sie das Spiel wirklich verlassen?",
                confirmText: "Verlassen",
                confirmColor: "danger",
                onConfirm: async () => {
                    await apiClient.api.gameSessionsPlayersDelete($game.gameId, $game.selfId);
                    goto(routes.menu());
                }
            });
        }}>Spiel verlassen</button>

        {#if config.allowSessionSharing}
            <button class="btn btn-warning w-100 ms-2" type="button" onclick={() => {
                const sessionUrl = webUrl + routes.gameDirectJoin($game.gameId, $game.selfId, getPlayerToken($game.gameId, $game.selfId)!.playerToken);
                modalProvider.show({
                    title: "Spiel auf einem anderen Gerät fortsetzen",
                    contentText: `Öffnen Sie <a href="${sessionUrl}">diesen Link</a> auf dem Gerät wo die Sitzung fortgesetzt werden soll.<br>Nach dem wechsel können Sie diesen Tab schließen.`,
                    confirmText: "Gerät Gewechselt",
                    allowHtmlText: true
                })
            }}>Auf anderes Gerät wechseln</button>
        {/if}

        {#if $game.selfId === $game.gameMeta?.gameMaster && ($game.gameState ?? -2) > 0}
            <button class="btn btn-danger w-100 ms-2" type="button" onclick={() => modalProvider.show({
                    title: "Spiel beenden?",
                    contentText: "Möchten Sie die Runde wirklich beenden? (Spieler bleiben in der Sitzung)",
                    confirmText: "Beenden",
                    confirmColor: "danger",
                    onConfirm: async () => {
                        await gameHub.stopGame()
                        modalProvider.hide();
                    }
                })}>Runde beenden</button>

            {#if serverConfig.gameMasterSkipAllowed ?? false}
                <button class="btn btn-danger w-100 ms-2" type="button" onclick={() => modalProvider.show({
                    title: "Spieler Aktion überspringen?",
                    contentText: "Diese Funktion sollte nur genutzt werden, wenn ein Nutzer AFK ist. Das Ergebnis der Aktion wird im voraus ausgewertet egal, ob Spieler noch nicht abgestimmt haben.",
                    confirmText: "Überspringen",
                    confirmColor: "danger",
                    onConfirm: async () => {
                        await gameHub.cancelCurrentAction();
                        modalProvider.hide();
                    }
                })}>Aktuelle Aktion beenden</button>
            {/if}
        {/if}
    </div>
</div>

<div class="header-fixed large-vp d-flex">
    <button class="btn btn-secondary d-flex align-items-center me-2" type="button" use:tooltip={{title: canEditSettings
            ? "Spieleinstellungen" : "Spieleinstellungen ansehen"}} onclick={() => modalProvider.show({
        title: canEditSettings ? "Spieleinstellungen" : "Spieleinstellungen (nur ansehen)",
        content: gameOptionsModalContent,
        confirmText: "Schließen",
        canDismiss: false,
        closeOnConfirm: true
    })}><span class="material-symbols-outlined">settings</span></button>
    <button class="btn btn-secondary d-flex align-items-center" type="button" use:tooltip={{title: "Logs Ein-/Ausblenden"}}
            onclick={() => document.getElementById('logSidePanel')!.classList.toggle('is-hidden')}>
        <span class="material-symbols-outlined">manage_search</span>
    </button>
</div>
<aside id="logSidePanel" class="log-panel-fixed large-vp bg-dark-subtle overflow-auto p-2 is-hidden">
    {#each $game.gameLogs as message}
        <p>{renderMessage(message)}</p>
    {/each}
</aside>

<div id="client-disconnected" class="reconnect-ui position-fixed top-0 start-0 w-100 h-100 d-flex flex-column justify-content-center align-items-center d-none">
    <div class="spinner-border m-4" style="width: 3rem; height: 3rem;" role="status">
        <span class="visually-hidden">Verbinden...</span>
    </div>
    <strong>Verbindung zum Server verloren. Verbinden...</strong>
</div>

<style>
    @media (max-width: 576px) {
        .main-content {
            width: 100%;
            max-width: 100vw;
        }

        .large-vp { visibility: hidden; }
        .small-vp { visibility: visible; }
    }

    @media (min-width: 576px) and (max-width: 1200px) {
        .main-content {
            width: 100%;
            max-width: 35rem;
        }

        .large-vp { visibility: hidden; }
        .small-vp { visibility: visible; }
    }

    @media (min-width: 1200px) {
        .main-content {
            width: 100%;
            max-width: 45rem;
        }

        .large-vp { visibility: visible; }
        .small-vp { visibility: hidden; }
    }
    
    .header-fixed {
        position: fixed;
        top: 10px;
        right: 20px;
    }

    .log-panel-fixed {
        position: fixed;
        top: 60px;
        right: 20px;
        bottom: 20px;
        width: 360px;
        border-radius: 16px;
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);

        transition: transform 0.3s ease;
        transform: translateX(0);
    }

    .log-panel-fixed.is-hidden {
        transform: translateX(calc(100% + 40px)); /* 100% Breite + Abstand rechts + Abstand zum Rand */
    }

    .reconnect-ui {
        background: rgba(0, 0, 0, 0.2);
        backdrop-filter: blur(8px);
        -webkit-backdrop-filter: blur(8px);
        z-index: 1050;
    }
</style>