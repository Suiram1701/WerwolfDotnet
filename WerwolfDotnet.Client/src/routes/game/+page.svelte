<script lang="ts">
    import { getContext, onMount } from "svelte";
    import { page } from "$app/state"
    import { goto } from "$app/navigation";
    import { type Readable } from "svelte/store";
    import { ActionType, Api, GameState } from "../../Api";
    import { storePlayerToken, getPlayerToken, removePlayerToken } from "../../stores/gameSessionStore";
    import { gamePageState as game } from "../../stores/pageStateStore";
    import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
    import { GameHub } from "../../gameHub";
    import { roleDescriptions, roleNames } from "../../textes/roles";
    import { actionDescriptions, actionNames } from "../../textes/actions";
    import { tooltip } from "$lib/actions/tooltip";
    import ModalProvider from "$lib/components/ModalProvider.svelte";
    import PageTitle from "$lib/components/PageTitle.svelte";
    import PlayerList from "$lib/components/PlayerList.svelte";
    import GameSettings from "$lib/components/GameSettings.svelte";
    import { config } from "../../config";

    const webUrl = page.url.protocol + "//" + page.url.host;     // Port is part of the host
    
    let modalProvider: ModalProvider;
    const modalAccessor = getContext<Readable<ModalProvider>>("modalProvider");
    modalAccessor.subscribe(m => modalProvider = m);

    const apiClient = new Api({ baseUrl: config.apiEndpoint, securityWorker: (data: unknown) => {
        // @ts-ignore
        return { headers: { "Authorization": `Bearer ${data.token}` } };
    }});
    config.retrieveConfigAsync(apiClient);

    let showAllPlayers = $state(false);
    
    let enoughPlayers = $derived($game.players.length >= (config.getClientConfig()?.minimumPlayers ?? 0));
    let everyOneReady = $derived(config.getClientConfig()?.canStartWhenNotReady || $game.playersReady.length === $game.players.length);

    let isWerwolfKilling = $derived($game.currentAction?.type === ActionType.WerwolfKilling);
    let canEditSettings = $derived($game.gameMeta?.gameMaster === $game.selfId && ($game.gameState ?? 0) <= 0);
    
    let connection: HubConnection;
    let gameHub: GameHub;
    onMount(() => {
        game.update(s => {
            s.gameId = Number.parseInt(page.url.searchParams.get("sessionId") ?? "");
            s.selfId = Number.parseInt(page.url.searchParams.get("playerId") ?? "");
            return s;
        });
        
        let playerToken = getPlayerToken($game.gameId, $game.selfId);
        if (page.url.hash.startsWith("#auth=")) {
            playerToken = page.url.hash.substring(6);
            storePlayerToken($game.gameId, $game.selfId, playerToken);
            goto(`/game?sessionId=${$game.gameId}&playerId=${$game.selfId}`);     // Remove auth secret from URL
        }
        else if (playerToken === undefined) {
            if ($game.gameId > 0)
                goto(`/?gameId=${$game.gameId}`);
            else
                goto("/");
            return;
        }
        
        apiClient.setSecurityData({ token: playerToken });
        connection = new HubConnectionBuilder()
            .withUrl(`${config.apiEndpoint}/signalr/game`, { accessTokenFactory: () => playerToken })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();
        connection.start()
            .then(() => gameHub = new GameHub(connection, modalProvider))
            .catch(err => {
                console.log("An error occurred while trying to connect to the game API!", err)
                goto("/");
            });
        return () => connection.stop();
    });
</script>

{#snippet gameOptionsModalContent()}
    <GameSettings readonly={!canEditSettings} apiClient={apiClient} />
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
        <p>Direktes Beitreten ist auch über <a href="{webUrl}?gameId={$game.gameId}">diesen Link</a> möglich.</p>
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

    <button class="btn btn-secondary main-content mb-3" type="button" onclick={() => {
        modalProvider.show({
            title: canEditSettings ? "Spieleinstellungen" : "Spieleinstellungen (nur ansehen)",
            content: gameOptionsModalContent,
            confirmText: "Schließen",
            canDismiss: false,
            closeOnConfirm: true
        });
    }}>{canEditSettings ? "Spieleinstellungen" : "Spieleinstellungen ansehen"}</button>
    
    <!-- Admin buttons -->
    {#if $game.selfId === $game.gameMeta?.gameMaster && ($game.gameState ?? -2) <= 0}
        <div class="d-flex main-content mb-3">
            {#if enoughPlayers && everyOneReady}
                <button class="btn btn-primary w-100" type="button" onclick={async () => await gameHub.startGame()}>Spiel starten</button>
            {:else}
                <span class="d-inline-block w-100" use:tooltip={{ title: !enoughPlayers
                    ? `Es müssen mindestens ${config.getClientConfig()?.minimumPlayers} in einer Runde sein.`
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
    
    <div class="d-flex main-content mb-3">
        <!-- Leave button -->
        <button class="btn btn-danger w-100" type="button" onclick={() => {
            modalProvider.show({
                title: "Spiel verlassen?",
                contentText: "Möchten Sie das Spiel wirklich verlassen?",
                confirmText: "Verlassen",
                confirmColor: "danger",
                onConfirm: async () => {
                    await apiClient.api.gameSessionsPlayersDelete($game.gameId, $game.selfId);
                    goto("/");
                }
            });
        }}>Spiel verlassen</button>
        
        <button class="btn btn-warning w-100 ms-2" type="button" onclick={() => {
            const sessionUrl = `${webUrl}/game?sessionId=${$game.gameId}&playerId=${$game.selfId}#auth=${getPlayerToken($game.gameId, $game.selfId)}`;
            modalProvider.show({
                title: "Spiel auf einem anderen Gerät fortsetzen",
                contentText: `Öffnen Sie <a href="${sessionUrl}">diesen Link</a> auf dem Gerät wo die Sitzung fortgesetzt werden soll.<br>Nach dem wechsel können Sie diesen Tab schließen.`,
                confirmText: "Gerät Gewechselt",
                allowHtmlText: true
            })
        }}>Auf anderes Gerät wechseln</button>

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

            {#if config.getClientConfig()?.gameMasterSkipAllowed ?? false}
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