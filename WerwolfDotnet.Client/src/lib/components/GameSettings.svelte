<script lang="ts">
    import {onMount} from "svelte";
    import {type Api, CauseOfDeath, type ClientConfigDto, type GameOptionsDto, Role} from "../../Api";
    import {gamePageState as gameState} from "../../stores/pageStateStore"
    import {roleNames} from "../../textes/roles";
    import {causeOfDeaths} from "../../textes/causeOfDeaths";
    import {tooltip} from "$lib/actions/tooltip";
    import {config} from "../../config";

    let { readonly, apiClient }: { readonly: boolean, apiClient: Api<unknown> } = $props();
    
    let cfg: ClientConfigDto = $state({});
    let options: GameOptionsDto = $state({});
    let totalRoles = $derived(Object.values((options.amountOfRoles ?? {})).reduce((previous: number, value: number) => previous + value, 0));

    onMount(() => {
        cfg = config.getClientConfig()!;     // Already loaded by page
        apiClient.api.gameSessionsSettingsDetail($gameState.gameId)
            .then(response => options = response.data)
            .catch(error => console.error("Failed to load game settings", error));
    });
    
    async function updateSettings(): Promise<void> {
        for (const key in options.amountOfRoles) {
            if (typeof options.amountOfRoles[key] !== "number")
                options.amountOfRoles[key] = 0;
        }
        if ((options.amountOfRoles!)[Role.Werwolf] < 1)
            (options.amountOfRoles!)[Role.Werwolf] = 1;
        if (options.revealRoleForCauses?.includes(CauseOfDeath.None) ?? false)
            options.revealRoleForCauses = [CauseOfDeath.None];
        
        const response = await apiClient.api.gameSessionsSettingsUpdate($gameState.gameId, options);
        if (response.ok)
            options = response.data;
        else
            console.error("Failed to update game settings", response.error)
    }
</script>

<h5>Sondereinstellungen:</h5>
<div class="form-check mb-3">
    <input class="form-check-input" id="explodingWitchHome" type="checkbox" bind:checked={options.explodingWitchHome}
           onchange={updateSettings} readonly={readonly} disabled={readonly}>
    <label class="form-check-label" for="explodingWitchHome">Explodierendes Hexenhaus</label>
</div>
<div class="form-check mb-3">
    <input class="form-check-input" id="hunterMustKill" type="checkbox" bind:checked={options.hunterMustKill}
           onchange={updateSettings} readonly={readonly} disabled={readonly}>
    <label class="form-check-label" for="hunterMustKill">Jäger muss töten</label>
</div>
<div class="form-check mb-3">
    <input class="form-check-input" id="mayorDecidesNextMayor" type="checkbox" bind:checked={options.mayorDecidesNextMayor}
           onchange={updateSettings} readonly={readonly} disabled={readonly}>
    <label class="form-check-label" for="mayorDecidesNextMayor">Bürgermeister entscheidet den nächsten Bürgermeister</label>
</div>

<h5>Rollen:</h5>
{#if options.amountOfRoles !== undefined && cfg.fixedRoleAmounts !== undefined}
    {#each Object.values(Role).filter(r => typeof r === "number" && r !== Role.None).sort() as role}
        <div class="row mb-1">
            <label class="form-label col-4" for="role{role.toString()}">{roleNames[role]}: </label>

            {#if role in (cfg.fixedRoleAmounts ?? {})}
                <input class="form-check-input roleCheckbox" id="role{role.toString()}" type="checkbox" readonly={readonly}
                       disabled={readonly} checked={options.amountOfRoles[role] > 0} onchange={async e => {
                           (options.amountOfRoles ?? {})[role] = e.currentTarget.checked ? (cfg.fixedRoleAmounts ?? {})[role] : 0;
                           await updateSettings();
                       }}>
            {:else}
                <input class="form-input col-2" id="role{role.toString()}" type="number" placeholder="0" bind:value={options.amountOfRoles[role]} 
                       min="{role === Role.Werwolf ? 1 : 0}" onchange={updateSettings} readonly={readonly} disabled={readonly}>
            {/if}
        </div>
    {/each}
{/if}
<p class="d-flex align-items-center {totalRoles > $gameState.players.length ? 'text-danger ' : ''}mt-2">
    Rollen Insgesamt: <b class="mx-1">{totalRoles}</b> {"<="} {$gameState.players.length}
    {#if totalRoles > $gameState.players.length}
        <span class="material-symbols-outlined ms-1" use:tooltip={{
            title: "Das Spiel kann nicht gestartet werden, da es mehr Rollen als Spieler gibt.",
            placement: "right"
        }}>info</span>
    {/if}
</p>
<p>Verbleibende Spieler bekommen die Dorfbewohner Rolle.</p>

<h5>Rolle bei Todesart anzeigen:</h5>
<select class="form-select mb-2" bind:value={options.revealRoleForCauses} onchange={updateSettings} disabled={readonly} multiple>
    <option value="{CauseOfDeath.None}">Keine</option>
    {#each Object.values(CauseOfDeath).filter(r => typeof r === "number" && r !== CauseOfDeath.None).sort() as cause}
        <option value="{cause}">{causeOfDeaths[cause]}</option>
    {/each}
</select>

{#if readonly}
    <button class="btn btn-outline-info" type="button" onclick="{async () => {
        const response = await apiClient.api.gameSessionsSettingsDetail($gameState.gameId);
        options = response.data;
    }}">
        Neuladen
    </button>
{/if}

<style>
    .roleCheckbox {
        padding-right: 0;     /* Bootstrap row would stretch the checkbox so overwrite it */
    }
</style>