<script lang="ts">
    import { Api, type GameDto, type JoinGameDto, type JoinedGameDto } from "../../../Api";
    
    let { id, preSetGame, apiClient }: { id: string, preSetGame: GameDto | null, apiClient: Api<any> } = $props();
    
    let gameId : number | null = $derived(preSetGame?.id ?? null);
    let gameIdLocked : boolean = $derived(preSetGame !== null);
    let playerName: string, password : string;
    let passwordRequired : boolean = $derived(preSetGame?.protected ?? true);
    
    async function onJoin(): Promise<void> {
        
    }
</script>

<div {id} class="modal fade" tabindex="-1">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Spiel beitreten</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label" for="joinGameId">Spielcode</label>
                    {#if gameIdLocked}
                        <input class="form-control" id="joinGameId" type="number" value="{gameId}" disabled readonly />
                    {:else}
                        <input class="form-control" id="joinGameId" type="number" bind:value={gameId} min="0" max="999999" />
                    {/if}
                    
                </div>

                <div class="mb-3">
                    <label class="form-label" for="joinPlayerName">Spielername</label>
                    <input class="form-control" id="joinPlayerName" type="text" bind:value={playerName} />
                </div>

                {#if passwordRequired}
                    <div class="mb-3">
                        <label class="form-label" for="joinPassword">Passwort</label>
                        <input class="form-control" id="joinPassword" type="password" bind:value={password} />
                    </div>
                {/if}
            </div>
            <div class="modal-footer">
                <button class="btn btn-primary" type="button" onclick={onJoin}>Beitreten</button>
            </div>
        </div>
    </div>
</div>