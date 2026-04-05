<script lang="ts">
    import { getContext } from "svelte";
    import { type Readable } from "svelte/store";
    import { ActionType, Api, GameState, type PlayerDto, PlayerRelation } from "../../Api";
    import { gamePageState as state } from "../../stores/pageStateStore";
    import { roleNames } from "../../textes/roles";
    import { tooltip } from "$lib/actions/tooltip";
    import ModalProvider from "$lib/components/ModalProvider.svelte";

    let { apiClient }: { apiClient: Api<unknown> } = $props();
    
    let sortedPlayers = $derived($state.gameState === GameState.GameWon ? $state.players.sort(p => p.role ?? 0) : $state.players);
    let isWerwolfKilling = $derived($state.currentAction?.type === ActionType.WerwolfKilling);
    
    let modalProvider: ModalProvider;
    const modalAccessor = getContext<Readable<ModalProvider>>("modalProvider");
    modalAccessor.subscribe(m => modalProvider = m);
    
    function getPlayerCSSClasses(player: PlayerDto): string {
        if (player.id === $state.selfId)
            return "list-group-item-success";
        else if (!player.alive)
            return "list-group-item-secondary";
        else if ($state.playerRelations[player.id!]?.includes(PlayerRelation.Ally) ?? false)
            return "list-group-item-info";
        else if ($state.playerRelations[player.id!]?.includes(PlayerRelation.Lover) ?? false)
            return "list-group-item-danger";
        return "";
    }
</script>

{#each sortedPlayers as player}
    <li class="list-group-item {getPlayerCSSClasses(player)} d-flex align-items-center">
        {#if $state.currentAction === null}
            {player.name}
        {:else}
            <!-- Decide between radio (one selection) and checkbox (multiple selections) -->
            {#if $state.currentAction.maximum === 1}
                <input class="form-check-input me-2" id="playerAction{player.id}" type="radio" value="{player.id}" bind:group={$state.selectedPlayers[0]}
                       name="playerAction" disabled="{!$state.currentAction.votablePlayers?.includes(player.id ?? -1)}" />
            {:else}
                <input class="form-check-input me-2" id="playerAction{player.id}" type="checkbox" value="{player.id}" bind:group={$state.selectedPlayers}
                       name="playerAction" disabled="{!$state.currentAction.votablePlayers?.includes(player.id ?? -1)}" />
            {/if}
            <label class="form-check-label" for="playerAction{player.id}">{player.name}</label>
        {/if}

        {#if $state.gameMeta?.gameMaster === player.id}
            <span class="material-symbols-outlined icon-filled ms-2" use:tooltip={{ title: "Game Master", placement: "right" }}>crown</span>
        {/if}

        {#if $state.gameMeta?.mayor === player.id}
            <span class="material-symbols-outlined icon-filled ms-2" use:tooltip={{ title: "Bürgermeister", placement: "right" }}>star</span>
        {/if}
        
        {#if !player.alive}
            <span class="material-symbols-outlined icon-filled ms-2">skull</span>
        {/if}
        
        {#if $state.playerRelations[player.id ?? -1]?.includes(PlayerRelation.Lover) ?? false}
            <span class="material-symbols-outlined icon-filled ms-2" use:tooltip={{
                title: "Geliebter: Ihr seid ineinander verliebt. Stirbt dieser Spieler wirst du auch sterben.",
                placement: "right"
            }}>favorite</span>
        {/if}

        {#if $state.playerRelations[player.id ?? -1]?.includes(PlayerRelation.Ally) ?? false}
            {#if ($state.selfRole ?? 0) > 0}
                <span class="material-symbols-outlined icon-filled ms-2" use:tooltip={{
                    title: "Verbündeter: Ihr seid auf der selben. Ihr spielt gemeinsam um die Werwölfe zu besiegen.",
                    placement: "right"
                }}>diversity_3</span>
            {:else}
                <span class="material-symbols-outlined icon-filled ms-2" use:tooltip={{
                    title: "Verbündeter: Ihr beide seid Werwölfe und spielt gemeinsam gegen das Dorf.",
                    placement: "right"
                }}>nightlight</span>
            {/if}
        {/if}

        {#if player.role !== undefined && player.role !== null }
            <span class="material-symbols-outlined mx-1">arrow_right_alt</span>
            {roleNames[player.role]}
        {/if}

        <div class="flex-grow-1"></div>
        {#if (player.id ?? 0) in $state.playerToVotes && $state.playerToVotes[player.id ?? 0].length > 0}
        <span class="badge text-bg-secondary" use:tooltip={{
            title: $state.playerToVotes[player.id ?? 0].map(id => {
                const name = $state.players.find(p => p.id === id)?.name;
                return $state.gameMeta?.mayor === id && isWerwolfKilling ? `2x ${name}` : name;
            }).join(', '),
            placement: "top"
        }}>
            {#if $state.playerToVotes[player.id ?? 0].includes($state.gameMeta?.mayor ?? -1) && isWerwolfKilling}     <!-- Include the mayor vote -->
                {$state.playerToVotes[player.id ?? 0].length + 1}
            {:else}
                {$state.playerToVotes[player.id ?? 0].length}
            {/if}
        </span>
        {/if}

        {#if $state.selfId === $state.gameMeta?.gameMaster}
            <button type="button" class="btn btn-sm btn-{player.id === $state.selfId ? 'secondary' : 'danger'} w-auto ms-3" onclick={() => {
            if (player.id === $state.selfId)
                return;
            modalProvider.show({
                title: "Spieler kicken?",
                contentText: "Möchten Sie diesen Spieler wirklich aus dem Spiel werden?",
                confirmText: "Kicken",
                confirmColor: "danger",
                onConfirm: () => apiClient.api.gameSessionsPlayersDelete($state.gameId, player.id ?? -1),
                closeOnConfirm: true
            });
        }} disabled="{player.id === $state.selfId}">Kicken</button>
        {/if}
    </li>
{/each}

<style>
    .icon-filled {
        font-variation-settings: 'FILL' 1;
    }
</style>