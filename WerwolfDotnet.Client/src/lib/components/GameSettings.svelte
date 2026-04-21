<script lang="ts">
    import { onMount } from "svelte";
    import { type Api, CauseOfDeath, type ClientConfigDto, type GameOptionsDto, Role } from "../../Api";
    import { gamePageState as gameState } from "../../stores/pageStateStore"
    import { roleNames, roleDescriptions } from "../../textes/roles";
    import { causeOfDeaths } from "../../textes/causeOfDeaths";
    import { config } from "../../config";
    import Tooltip from "$lib/components/Tooltip.svelte";

    let { readonly, apiClient }: { readonly: boolean, apiClient: Api<unknown> } = $props();
    
    let cfg: ClientConfigDto = $state({});
    let options: GameOptionsDto = $state({});
    let totalRoles = $derived(Object.values((options.amountOfRoles ?? {})).reduce((previous: number, value: number) => previous + value, 0));

    let pollId: NodeJS.Timeout;
    onMount(() => {
        cfg = config.getClientConfig()!;     // Already loaded by page
        
        pollSettings();     // show.bs.modal isn't called when the modal is first rendered
        document.addEventListener("show.bs.modal", _ => pollSettings());
        document.addEventListener("hide.bs.modal", _ => clearInterval(pollId));
        return () => clearInterval(pollId);
    });
    
    function pollSettings(): void {
        apiClient.api.gameSessionsSettingsDetail($gameState.gameId)
            .then(response => options = response.data)
            .catch(error => console.error("Failed to load game settings", error));
        
        if (!readonly)     // No need to poll when you're the only one who can edit.
            return;
        pollId = setInterval(async () => {
            const response = await apiClient.api.gameSessionsSettingsDetail($gameState.gameId);
            if (response.ok) {
                options = response.data;
                return;
            }

            console.error("Failed to load game settings", response.error);
        }, config.apiPollInterval);
    }
    
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
<div class="form-check d-flex mb-3">
    <input class="form-check-input" id="seerSeesRole" type="checkbox" bind:checked={options.seerSeesRole}
           onchange={updateSettings} readonly={readonly} disabled={readonly}>
    <label class="form-check-label mx-2" for="seerSeesRole">Seher sieht genaue Rolle</label>
    <Tooltip title="Wenn die Option deaktiviert ist wird nur die Gesinnung der Person (Werwölfe oder Dorfbewohner) dem Seher angezeigt. Andernfalls wird die aktuelle Rolle des Spielers angezeigt." />
</div>
<div class="form-check d-flex mb-3">
    <input class="form-check-input" id="explodingWitchHome" type="checkbox" bind:checked={options.explodingWitchHome}
           onchange={updateSettings} readonly={readonly} disabled={readonly}>
    <label class="form-check-label  mx-2" for="explodingWitchHome">Explodierendes Hexenhaus</label>
    <Tooltip title="Eine Spielvariante, bei der sobald die Hexe stirbt ihr Haus explodiert und damit die Spieler links (oben) und rechts (unten) von ihr tötet." />
</div>
<div class="form-check d-flex mb-3">
    <input class="form-check-input" id="hunterMustKill" type="checkbox" bind:checked={options.hunterMustKill}
           onchange={updateSettings} readonly={readonly} disabled={readonly}>
    <label class="form-check-label  mx-2" for="hunterMustKill">Jäger muss töten</label>
    <Tooltip title="Entscheidet darüber, ob der Jäger, wenn er stirbt, einen anderen Spieler töten muss oder es ihm freigestellt ist, ob er jemanden mit in den tot reißt oder nicht." />
</div>
<div class="form-check d-flex mb-3">
    <input class="form-check-input" id="mayorDecidesNextMayor" type="checkbox" bind:checked={options.mayorDecidesNextMayor}
           onchange={updateSettings} readonly={readonly} disabled={readonly}>
    <label class="form-check-label  mx-2" for="mayorDecidesNextMayor">Bürgermeister entscheidet bei Tod über den nächsten Bürgermeister</label>
    <Tooltip title="Wenn die Option aktiviert ist entscheidet der Bürgermeister sobald er stirbt den Nächten, andernfalls kommt es am darauf folgenden Tag zu einer Neuwahl." />
</div>

<h5>Rollen:</h5>
{#if options.amountOfRoles !== undefined && cfg.fixedRoleAmounts !== undefined}
    {#each Object.values(Role).filter(r => typeof r === "number" && r !== Role.None).sort((a, b) => a - b) as role}
        <div class="row mb-1">
            <label class="form-label d-flex col-6 col-sm-5" for="role{role.toString()}">
                {roleNames[role]}<Tooltip classNames="ms-auto" title={roleDescriptions[role]} />:
            </label>

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
    Rollen insgesamt: <b class="mx-1">{totalRoles}</b> {"<="} {$gameState.players.length}
    <Tooltip classNames="ms-1" title="Die Anzahl der ausgewählten Rollen muss kleiner gleich die Anzahl der Spieler sein. 
        Wenn mehr Rollen ausgewählt sind kann das Spiel nicht gestartet werden, wenn weniger wird mit Dorfbewohnern aufgefüllt." />
</p>
<p>Verbleibende Spieler bekommen die Dorfbewohner Rolle.</p>

<h5>Rollen aufdecken bei Folgenden Todesarten:</h5>
<div class="d-flex flex-column">
    {#each Object.values(CauseOfDeath).filter(r => typeof r === "number" && r !== CauseOfDeath.None).sort() as cause}
        <div class="form-check mb-2">
            <input class="form-check-input" id="causeOfDeath{cause}" type="checkbox" readonly={readonly} disabled={readonly}
                   checked={options.revealRoleForCauses?.includes(cause)} onchange={async e => {
                       if (e.currentTarget.checked && !options.revealRoleForCauses?.includes(cause))
                           options.revealRoleForCauses?.push(cause);
                       else
                           options.revealRoleForCauses = options.revealRoleForCauses?.filter(r => r !== cause);
                       await updateSettings();
                   }}>
            <label class="form-check-label" for="causeOfDeath{cause}">{causeOfDeaths[cause]}</label>
        </div>
    {/each}
</div>

<style>
    .roleCheckbox {
        padding-right: 0;     /* Bootstrap row would stretch the checkbox so overwrite it */
    }
</style>