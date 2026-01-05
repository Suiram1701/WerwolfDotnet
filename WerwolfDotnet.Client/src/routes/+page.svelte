<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { onMount, getContext } from "svelte";
    import { type Readable } from "svelte/store";
    import { config } from "../config";
    import { Api, type HttpResponse, type GameDto, type JoinGameDto, type JoinedGameDto} from "../Api";
    import { storePlayerToken } from "../gameSessionStore";
    import PageTitle from "./components/PageTitle.svelte"
    import GameCard from "./components/GameCard.svelte";
    import ModalProvider from "./components/ModalProvider.svelte";
    
    let modalProvider: ModalProvider;
    const modalAccessor = getContext<Readable<ModalProvider>>("modalProvider");
    modalAccessor.subscribe(m => modalProvider = m);
    
    const apiClient = new Api({ baseUrl: config.apiEndpoint });
    let games: GameDto[] | null = $state(null);

    let gameId: number | undefined = $state(), gameIdLocked: boolean = $state(false);
    let playerName: string = $state("");
    let password: string = $state(""), passwordRequired: boolean = $state(true);
    
    function onCreateGame() {
        const request: JoinGameDto = {
            playerName: playerName,
            gamePassword: password
        };
        apiClient.api.gameSessionsCreate(request)
            .then(response => joinGame(response.data))
            .catch((response: HttpResponse<JoinedGameDto>) => {
                if (response.status === 400) 
                    document.getElementById("creatingPlayerName")!.classList.add("is-invalid");     // No need to reset because it's the only input for this form.
                else
                    modalProvider.showSimple("Fehler bei der Anfrage", `${response.status}: ${response.statusText}`);
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
                        modalProvider.showSimple("Fehler bei der Anfrage", `${response.status}: ${response.statusText}`);
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
            modalProvider.showSimple("Spiel verlassen", `Sie wurden vom Game master aus dem Spiel geworfen.`);
        }
        
        if (page.url.searchParams.get("gameId") !== null) {
            const urlGameId = Number.parseInt(page.url.searchParams.get("gameId")!);
            apiClient.api.gameSessionsDetail(urlGameId)
                .then(response => {
                    if (!response.data.canJoin) {
                        if ((response.data.playerCount ?? 0) >= (response.data.maxPlayerCount ?? 0))
                            modalProvider.showSimple("Spiel beitritt nicht möglich", "Das Spiel hat bereits die maximale Anzahl an Spielern erreicht!");
                        else
                            modalProvider.showSimple("Spiel beitritt nicht möglich", "Das Spiel läuft bereits!");
                        return;
                    }
                    
                    gameId = response.data.id;
                    gameIdLocked = true;
                    password = "";
                    passwordRequired = response.data.protected ?? true;

                    modalProvider.show("Spiel beitreten", joinModalContent, true, "Beitreten", "primary", onJoinGame);
                })
                .catch(response => {
                    if (response.status === 404)
                        return;
                    modalProvider.showSimple("Fehler bei der Anfrage", `${response.status}: ${response.statusText}`);
                });
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

{#snippet createModalContent()}
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
{/snippet}

{#snippet joinModalContent()}
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
{/snippet}

<PageTitle title="Werwolf - Lobbies" />

<div class="row d-flex flex-wrap">
    <button class="col btn btn-primary m-1 start-button" type="button" onclick={() => {
            password = "";
            modalProvider.show("Neues Spiel erstellen", createModalContent, true, "Erstellen", "primary", onCreateGame);
        }}>Neues Spiel erstellen</button>

    <button class="col btn btn-secondary m-1 start-button" type="button" onclick={() => {
            gameId = undefined;
            gameIdLocked = false;
            password = "";
            passwordRequired = true;
            
            modalProvider.show("Spiel beitreten", joinModalContent, true, "Beitreten", "primary", onJoinGame);
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

                        modalProvider.show("Spiel beitreten", joinModalContent, true, "Beitreten", "primary", onJoinGame);
                    }} />
            {/each}
        </div>
    {:else}
        <h6>Es gibt noch keine Spiele</h6>
    {/if}
{:else}
    <h6>Vorhandene Spiele können aufgrund der Serverkonfiguration nicht angezeigt werden</h6>
{/if}

<style>
    .start-button {
        min-width: 16rem;
    }
</style>