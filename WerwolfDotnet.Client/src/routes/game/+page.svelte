<script lang="ts">
    import { getContext, onMount } from "svelte";
    import { page } from "$app/state"
    import { goto } from "$app/navigation";
    import { type Readable } from "svelte/store";
    import { Api, GameState, type PlayerDto } from "../../Api";
    import { getPlayerToken } from "../../stores/gameSessionStore";
    import { gamePageState as state } from "../../stores/pageStateStore";
    import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
    import { GameHub } from "../../gameHub";
    import { roleDescriptions, roleNames } from "../../textes/roles";
    import { actionDescriptions, actionNames } from "../../textes/actions";
    import { tooltip } from "$lib/actions/tooltip";
    import ModalProvider from "$lib/components/ModalProvider.svelte";
    import PageTitle from "$lib/components/PageTitle.svelte";
    import PlayerList from "$lib/components/PlayerList.svelte";
    import { config } from "../../config";

    const webUrl = page.url.protocol + "//" + page.url.host;     // Port is part of the host
    
    let modalProvider: ModalProvider;
    const modalAccessor = getContext<Readable<ModalProvider>>("modalProvider");
    modalAccessor.subscribe(m => modalProvider = m);

    const apiClient = new Api({ baseUrl: config.apiEndpoint, securityWorker: (data: unknown) => {
        // @ts-ignore
        return { headers: { "Authorization": `Bearer ${data.token}` } };
    }});
    
    let connection: HubConnection;
    let gameHub: GameHub;
    onMount(() => {
        state.update(s => {
            s.gameId = Number.parseInt(page.url.searchParams.get("sessionId") ?? "");
            s.selfId = Number.parseInt(page.url.searchParams.get("playerId") ?? "");
            return s;
        });
        
        const playerToken = getPlayerToken($state.gameId, $state.selfId);
        if (playerToken === undefined) {
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

<PageTitle title="Werwolf - Spiel {$state.gameId}" />

{#if $state.selfRole !== undefined}
    <div class="accordion w-100 mb-4" id="collapseRoleParent">
        <div class="accordion-item">
            <h2 class="accordion-header">
                <button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#collapseRole" aria-controls="collapseRole">
                    Ihre Rolle
                </button>
            </h2>
            <div id="collapseRole" class="accordion-collapse collapse" data-bs-parent="#collapseRoleParent">
                <div class="accordion-body">
                    <h5>{roleNames[$state.selfRole]}</h5>
                    {roleDescriptions[$state.selfRole]}
                </div>
            </div>
        </div>
    </div>
{/if}

<div class="text-center mb-4">
    {#if $state.currentAction !== null}
        <h5>{actionNames[$state.currentAction.type ?? 0] || "undefined"}</h5>
        <p>{actionDescriptions[$state.currentAction.type ?? 0] || "Dies solltest du eigentlich nicht sehen :)"}</p>
    {:else if $state.gameState === GameState.Preparation}
        <p>Andere Spieler können beitreten indem Sie diese Website (<a href="{webUrl}">{page.url.host}</a>) gehen und den Spielcode <b>{$state.gameId?.toString().padStart(6, '0')}</b> eingeben.</p>
        <p>Direktes beitreten ist auch über <a href="{webUrl}?gameId={$state.gameId}">diesen Link</a> möglich.</p>
    {:else if $state.gameState === GameState.Day}
        <p>Der Tag ist angebrochen. Diskutiert und entscheidet euch für einen Spieler, der am Abend hingerichtet werden soll.</p>
    {:else if $state.gameState === GameState.Night}
        <p>Die Nacht ist angebrochen! Alle gehen schlafen, außer den Werwölfen...</p>
    {:else if $state.gameState === GameState.GameWon}
        <p>
            Die Spielrunde ist zu ende. Frage den Game-Master ob dieser die Runde beendet und eine neue beginnt. <br>
            <small>Fall Bugs, Glitches oder andere Fehler aufgetreten sind können diese gerne per <a href="https://github.com/Suiram1701/WerwolfDotnet/issues" target="_blank">GitHub</a> gemeldet werden :)</small>
        </p>
    {/if}
</div>

<div class="flex-grow-1 container-fluid d-flex flex-column align-items-center">
    <!-- Player display and selection -->
    <ul class="list-group main-content mb-4">
        <PlayerList apiClient={apiClient} />
        
        {#if $state.currentAction !== null && $state.currentAction.minimum === 0}
            <li class="list-group-item d-flex align-items-center d-flex align-items-center">
                {#if $state.currentAction.maximum === 1}
                    <input class="form-check-input me-2" id="playerAction-NoOne" name="playerAction" type="radio"
                           checked="{$state.selectedPlayers.length === 0}" onclick={() => state.update(s => {
                               s.selectedPlayers = [];
                               return s;
                           })}>
                {/if}
                <label class="form-check-label" for="playerAction-NoOne">Niemand auswählen</label>

                <div class="flex-grow-1"></div>
                {#if $state.emptyVotedPlayers.length > 0}
                    <span class="badge text-bg-secondary" use:tooltip={{
                        title: $state.emptyVotedPlayers.map(id => {
                            const name = $state.players.find(p => p.id === id)?.name;
                            return $state.gameMeta?.mayor === id ? `2x ${name}` : name;
                        }).join(', '),
                        placement: "top"
                    }}>
                        {#if $state.emptyVotedPlayers.includes($state.gameMeta?.mayor ?? -1)}     <!-- Include the mayor vote -->
                            {$state.emptyVotedPlayers.length + 1}
                        {:else}
                            {$state.emptyVotedPlayers.length}
                        {/if}
                    </span>
                {/if}
            </li>
        {/if}
    </ul>

    {#if $state.currentAction !== null}
        <button class="btn btn-primary main-content" type="button" onclick={() => gameHub.playerAction($state.selectedPlayers)}
                disabled="{$state.selectedPlayers.length < ($state.currentAction.minimum ?? 0) || $state.selectedPlayers.length > ($state.currentAction.maximum ?? 0)}">
            Abschicken
        </button>
    {/if}

    <div class="flex-grow-1"></div>
    
    <!-- Admin buttons -->
    {#if $state.selfId === $state.gameMeta?.gameMaster && ($state.gameState ?? -2) <= 0}
        <div class="d-flex main-content mb-3">
            <button class="btn btn-primary w-100" type="button" onclick={ async () => gameHub.startGame()} disabled="{$state.players.length < config.minimumPlayers}">Spiel starten</button>
            <button class="btn btn-info w-100 mx-2" type="button" onclick={ async () => await gameHub.shufflePlayers()}>Spieler durchmischen</button>

            {#if $state.gameState !== GameState.Locked}
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
                    await apiClient.api.gameSessionsPlayersDelete($state.gameId, $state.selfId, { secure: true });
                    goto("/");
                }
            });
        }}>Spiel verlassen</button>

        {#if $state.selfId === $state.gameMeta?.gameMaster && ($state.gameState ?? -2) > 0}
            <button class="btn btn-danger w-100 ms-2" type="button" onclick={() => modalProvider.show({
                    title: "Spiel beenden?",
                    contentText: "Möchten Sie das Spiel wirklich beenden? (Spieler bleiben in der Sitzung)",
                    confirmText: "Beenden",
                    confirmColor: "danger",
                    onConfirm: async () => {
                        await gameHub.stopGame()
                        modalProvider.hide();
                    }
                })}>Spiel beenden (zurück zur Lobby)</button>

            {#if config.gameMasterSkipAllowed}
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