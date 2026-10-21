<script lang="ts">
    import { goto } from "$app/navigation";
    import { gamePageState as game } from "../../stores/pageStateStore";
    import { tooltip } from "$lib/actions/tooltip";
    import Button from "$lib/components/Button.svelte";
    import { default as routes } from "../../routes/routes";
    import { config , getServerConfig } from "../../config";
    import { renderMessage } from "../../texts/logs";
    import GameSettings from "$lib/components/GameSettings.svelte";
    import {type Api, GameState} from "../../Api";
    import ModalProvider from "$lib/components/ModalProvider.svelte";
    import { type Readable } from "svelte/store";
    import { getContext } from "svelte";
    import {getPlayerToken} from "../../stores/gameSessionStore";
    import type {GameHub} from "../../gameHub";
    import {page} from "$app/state";

    let { apiClient, gameHub }: { apiClient: Api<any>, gameHub: GameHub } = $props();

    const webUrl = page.url.protocol + "//" + page.url.host;     // Port is part of the host
    
    let modalProvider: ModalProvider;
    const modalAccessor = getContext<Readable<ModalProvider>>("modalProvider");
    modalAccessor.subscribe(m => modalProvider = m);
    
    const serverConfig = getServerConfig();
    let enoughPlayers = $derived($game.players.length >= (serverConfig.minimumPlayers ?? 0));
    let everyOneReady = $derived(serverConfig.canStartWhenNotReady || $game.playersReady.length === $game.players.length);

    let canEditSettings = $derived($game.gameMeta?.gameMaster === $game.selfId && ($game.gameState ?? 0) <= 0);
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

<div class="small-vp d-flex main-content mt-3">
    <Button variant="secondary" icon="settings" class="w-100" onclick={() => modalProvider.show({
            title: canEditSettings ? "Spieleinstellungen" : "Spieleinstellungen (nur ansehen)",
            content: gameOptionsModalContent,
            confirmText: "Schließen",
            canDismiss: false,
            closeOnConfirm: true
        })}>{canEditSettings ? "Spieleinstellungen" : "Spieleinstellungen ansehen"}</Button>

    <Button variant="secondary" icon="manage_search" class="w-100 ms-2" onclick={() => modalProvider.show({
            title: "Spiel-Log",
            content: gameLogsModalContent,
            confirmText: "Schließen",
            size: "lg",
            canDismiss: false,
            closeOnConfirm: true
        })}>Spiel-Logs ansehen</Button>
</div>

<!-- Admin buttons -->
{#if $game.selfId === $game.gameMeta?.gameMaster && ($game.gameState ?? -2) <= 0}
    <div class="adm-btn main-content d-flex mt-3">
        {#if enoughPlayers && everyOneReady}
            <Button icon="play_arrow" class="w-100 my-1" onclick={async () => await gameHub.startGame()}>Spiel starten</Button>
        {:else}
                <span class="w-100 my-1" use:tooltip={{ title: !enoughPlayers
                    ? `Es müssen mindestens ${serverConfig.minimumPlayers} in einer Runde sein.`
                    : "Alle Spieler müssen bereit sein (Spieler, die AFK sind können auch gekicked werden)." }}>
                    <Button icon="play_arrow" class="w-100 h-100" disabled>Spiel starten</Button>
                </span>
        {/if}

        <Button variant="info" icon="shuffle" class="w-100 my-1 mx-2" onclick={ async () => await gameHub.shufflePlayers()}>Spieler durchmischen</Button>

        {#if $game.gameState !== GameState.Locked}
            <Button variant="warning" icon="lock" class="w-100 my-1" onclick={async () => await gameHub.setGameLocked(true)}>Beitreten blockieren</Button>
        {:else}
            <Button variant="success" icon="lock_open" class="w-100 my-1" onclick={async () => await gameHub.setGameLocked(false)}>Beitreten erlauben</Button>
        {/if}
    </div>
{/if}

<div class="d-flex main-content m-3">
    <!-- Leave button -->
    <Button variant="danger" icon="logout" class="w-100" onclick={() => modalProvider.show({
            title: "Spiel verlassen?",
            contentText: "Möchten Sie das Spiel wirklich verlassen?",
            confirmText: "Verlassen",
            confirmColor: "danger",
            onConfirm: async () => {
                await apiClient.api.gameSessionsPlayersDelete($game.gameId, $game.selfId);
                goto(routes.menu());
            }
        })}>Spiel verlassen</Button>

    {#if config.allowSessionSharing}
        <Button variant="warning" icon="move_location" class="w-100 ms-2" onclick={() => {
                const sessionUrl = webUrl + routes.gameDirectJoin($game.gameId, $game.selfId, getPlayerToken($game.gameId, $game.selfId)!.playerToken);
                modalProvider.show({
                    title: "Gerät wechseln",
                    contentText: `Öffnen Sie <a href="${sessionUrl}">diesen Link</a> auf dem Gerät wo die Sitzung fortgesetzt werden soll.<br>Nach dem wechsel können Sie diesen Tab schließen.`,
                    confirmText: "Gerät Gewechselt",
                    allowHtmlText: true
                })
            }}>Gerät wechseln</Button>
    {/if}

    {#if $game.selfId === $game.gameMeta?.gameMaster && ($game.gameState ?? -2) > 0}
        <Button variant="danger" class="w-100 ms-2" onclick={() => modalProvider.show({
                title: "Spiel beenden?",
                contentText: "Möchten Sie die Runde wirklich beenden? (Spieler bleiben in der Sitzung)",
                confirmText: "Beenden",
                confirmColor: "danger",
                onConfirm: async () => {
                    await gameHub.stopGame()
                    modalProvider.hide();
                }
            })}>Runde beenden</Button>

        {#if serverConfig.gameMasterSkipAllowed ?? false}
            <Button variant="danger" class="w-100 ms-2" onclick={() => modalProvider.show({
                    title: "Spieler Aktion überspringen?",
                    contentText: "Diese Funktion sollte nur genutzt werden, wenn ein Nutzer AFK ist. Das Ergebnis der Aktion wird im voraus ausgewertet egal, ob Spieler noch nicht abgestimmt haben.",
                    confirmText: "Überspringen",
                    confirmColor: "danger",
                    onConfirm: async () => {
                        await gameHub.cancelCurrentAction();
                        modalProvider.hide();
                    }
                })}>Aktuelle Aktion beenden</Button>
        {/if}
    {/if}
</div>

<style>
    @media (max-width: 576px) {
        :global(.main-content) {
            width: 100%;
            max-width: 100vw;
        }

        .adm-btn {
            flex-wrap: wrap;
        }
        .adm-btn > :global(button) {     /* :global is needed because Svelte thinks this is unused somehow */
            margin-left: 0 !important;   /* Overwrites m-2 from the shuffel player button */
            margin-right: 0 !important;
        }
    }
</style>