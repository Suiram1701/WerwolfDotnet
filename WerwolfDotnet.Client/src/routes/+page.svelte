<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { onMount } from "svelte";
    import { config } from "../config";
    import { Api, type HttpResponse, type GameDto, type JoinGameDto, type JoinedGameDto} from "../Api";
    import { storePlayerToken } from "../gameSessionStore";
    import PageTitle from "./components/PageTitle.svelte"
    import GameCard from "./components/GameCard.svelte";
    import Modal from "./components/Modal.svelte";
    
    const apiClient = new Api({ baseUrl: config.apiEndpoint });
    let games: GameDto[] | null = $state(null);

    let gameId: number | undefined = $state(), gameIdLocked: boolean = $state(false);
    let playerName: string = $state("");
    let password: string = $state(""), passwordRequired: boolean = $state(true);
    
    let errorText: string = $state("");
    
    let createModal: Modal;
    let joinModal: Modal;
    let errorModal: Modal;
    
    function onCreateGame() {
        const request: JoinGameDto = {
            playerName: playerName,
            gamePassword: password
        };
        apiClient.api.gameSessionsCreate(request)
            .then(response => joinGame(response.data))
            .catch((response: HttpResponse<JoinedGameDto>) => {
                if (response.status === 400) {
                    document.getElementById("creatingPlayerName")!.classList.add("is-invalid");     // No need to reset because it's the only input for this form.
                } else {
                    errorText = `${response.status}: ${response.statusText}`;
                    errorModal.show();
                }
            });
    }

    function onJoinGame() {
        for (const element of document.getElementsByClassName("form-control"))
            element.classList.remove("is-invalid");

        const request: JoinGameDto = {
            playerName: playerName,
            gamePassword: (passwordRequired ? password : null)
        };
        apiClient.api.gameSessionsJoinCreate(gameId ?? -1, request)
            .then(response => joinGame(response.data))
            .catch((response: HttpResponse<JoinedGameDto>) => {
                switch (response.status) {
                    case 400:
                        document.getElementById("joinPlayerName")!.classList.add("is-invalid");
                        break;
                    case 401:
                        document.getElementById("joinPassword")!.classList.add("is-invalid");
                        break;
                    case 404:
                        document.getElementById("joinGameId")!.classList.add("is-invalid");
                        break;
                    default:
                        errorText = response.status === 409
                            ? "Die angegebene Sitzung hat bereits die maximale Spieleranzahl erreicht."
                            : `${response.status}: ${response.statusText}`;
                        joinModal.hide();
                        errorModal.show();
                        break;
                }
            });
    }
    
    function joinGame(data: JoinedGameDto) {
        storePlayerToken(data.game.id ?? -1, data.self.id ?? 0, data.playerToken ?? "");
        goto(`/game?sessionId=${data.game.id}&playerId=${data.self.id}`);
    }
    
    let pollId: NodeJS.Timeout;
    onMount(() => {
        if (page.url.searchParams.get("kicked") !== null) {
            errorText = "Sie wurden aus der Sitzung geworfen vom Game master!"
            errorModal.show();
        }
        
        if (config.sessionsVisible) {
            games = [];
            apiClient.api.gameSessionsList()
                .then(response => {
                    games = response.data;

                    pollId = setInterval(async () => {
                        const response : HttpResponse<GameDto[], void> = await apiClient.api.gameSessionsList();
                        if (response.ok)
                            games = response.data;
                        else
                            console.error("Failed to poll game sessions!")
                    }, config.sessionsPollInterval * 1000);     // s to ms
                })
                .catch(_ => {
                    games = null;
                    console.warn("Can't poll game session because its disabled by the server (although its enabled for the client)"); 
                });
            return () => clearInterval(pollId);
        }
    });
</script>

<PageTitle>Werwolf - Lobbies</PageTitle>

<Modal bind:this={createModal} id="createModal" title="Neues Spiel erstellen" footer={createFooter}>
    <div class="mb-3">
        <label class="form-label" for="creatingPlayerName">Spielername</label>
        <input class="form-control" id="creatingPlayerName" type="text" bind:value={playerName} />
        <div class="invalid-feedback">Der angegebene Name ist ungültig.</div>
    </div>

    <div class="mb-3">
        <label class="form-label" for="creatingPassword">Passwort</label>
        <input class="form-control" id="creatingPassword" type="password" bind:value={password} aria-describedby="creatingPasswordHelp" />
        <div class="form-text" id="creatingPasswordHelp">
            Beitretende Spieler müssen dieses Passwort zum beitreten eingeben. Leer lassen um keines zu verlangen.
        </div>
    </div>
</Modal>
{#snippet createFooter()}
    <button class="btn btn-primary" type="button" onclick={onCreateGame}>Runde erstellen</button>
{/snippet}

<Modal bind:this={joinModal} id="joinModal" title="Spiel beitreten" footer={joinFooter}>
    <div class="mb-3">
        <label class="form-label" for="joinGameId">Spielcode</label>
        {#if gameIdLocked}
            <input class="form-control" id="joinGameId" type="number" value="{gameId}" disabled readonly />
        {:else}
            <input class="form-control" id="joinGameId" type="number" bind:value={gameId} min="0" max="999999" />
        {/if}
        <div class="invalid-feedback">Keine Spiele-Sitzung mit diesem Code konnte gefunden werden.</div>
    </div>

    <div class="mb-3">
        <label class="form-label" for="joinPlayerName">Spielername</label>
        <input class="form-control" id="joinPlayerName" type="text" bind:value={playerName} />
        <div class="invalid-feedback">Der gewählte Name ist ungültig oder bereits vergeben.</div>
    </div>

    {#if passwordRequired}
        <div class="mb-3">
            <label class="form-label" for="joinPassword">Passwort</label>
            <input class="form-control" id="joinPassword" type="password" bind:value={password} />
            <div class="invalid-feedback">Das angegebene Passwort ist nicht korrekt!</div>
        </div>
    {/if}
</Modal>
{#snippet joinFooter()}
    <button class="btn btn-primary" type="button" onclick={onJoinGame}>Beitreten</button>
{/snippet}

<Modal bind:this={errorModal} id="errorModal" title="Meldung" footer={errorFooter}>{errorText}</Modal>
{#snippet errorFooter()}
    <button class="btn btn-primary" type="button" data-bs-dismiss="modal">Verstanden</button>
{/snippet}

<div class="d-flex flex-column justify-content-center">
    <div class="row d-flex flex-wrap">
        <button class="col btn btn-primary m-1 start-button" type="button" onclick={() => {
            password = "";
            createModal.show();
        }}>Neues Spiel erstellen</button>
        
        <button class="col btn btn-secondary m-1 start-button" type="button" onclick={() => {
            gameId = undefined;
            gameIdLocked = false;
            password = "";
            passwordRequired = true;
            
            joinModal.show();
        }}>Spiel beitreten</button>
    </div>
    <hr />

    <h5>Vorhandene Spiele:</h5>
    {#if games !== null}
        {#if games?.length > 0}
            <div class="d-flex flex-wrap">
                {#each games as game}
                    <GameCard {game} onJoin={() => {
                        gameId = game.id;
                        gameIdLocked = true;
                        password = "";
                        passwordRequired = game.protected ?? true;

                        joinModal.show();
                    }} />
                {/each}
            </div>
        {:else}
            <h6>Es gibt noch keine Spiele</h6>
        {/if}
    {:else}
        <h6>Vorhandene Spiele können aufgrund der Serverkonfiguration nicht angezeigt werden</h6>
    {/if}
</div>

<style>
    .start-button {
        min-width: 16rem;
    }
</style>