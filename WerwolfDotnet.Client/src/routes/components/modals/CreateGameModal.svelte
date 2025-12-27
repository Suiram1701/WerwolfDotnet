<script lang="ts">
    import { Api, type GameDto, type JoinGameDto, type JoinedGameDto } from "../../../Api";

    let { id, apiClient }: { id: string, apiClient: Api<any> } = $props();
    let playerName: string, password: string;
    
    async function onCreate(): Promise<void> {
    }
</script>

<div {id} class="modal fade" tabindex="-1">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Neues Spiel erstellen</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label" for="creatingPlayerName">Spielername</label>
                    <input class="form-control" id="creatingPlayerName" type="text" bind:value={playerName} />
                </div>

                <div class="mb-3">
                    <label class="form-label" for="creatingPassword">Passwort</label>
                    <input class="form-control" id="creatingPassword" type="password" bind:value={password} aria-describedby="creatingPasswordHelp" />
                    <div class="form-text" id="creatingPasswordHelp">
                        Beitretende Spieler müssen dieses Passwort zum beitreten eingeben. Leer lassen um keines zu verlangen.
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-primary" type="button" onclick={onCreate}>Runde erstellen</button>
            </div>
        </div>
    </div>
</div>