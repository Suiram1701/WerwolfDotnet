<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { onMount, getContext } from "svelte";
    import { type Readable } from "svelte/store";
    import { config, getServerConfig } from "../config";
    import { Api, type HttpResponse, type GameDto, type JoinGameDto, type JoinedGameDto} from "../Api";
    import { getPlayerToken, getPlayerTokens, removePlayerToken, storePlayerToken } from "../stores/gameSessionStore";
    import { default as routes } from "./routes";
    import PageTitle from "$lib/components/PageTitle.svelte"
    import Button from "$lib/components/Button.svelte";
    import GameCard from "$lib/components/GameCard.svelte";
    import ModalProvider from "$lib/components/ModalProvider.svelte";
    
    let modalProvider: ModalProvider;
    const modalAccessor = getContext<Readable<ModalProvider>>("modalProvider");
    modalAccessor.subscribe(m => modalProvider = m);
    
    const apiClient = new Api({ baseUrl: config.apiEndpoint });
    
    let games: GameDto[] | null = $state([]);
    
    let gameId: number | undefined = $state(), gameIdLocked: boolean = $state(false);
    let playerName: string = $state("");
    let password: string = $state(""), passwordRequired: boolean = $state(true);
    
    function onCreateGame() {
        const errorMessage = getPlayerNameError();
        if (errorMessage !== null) {
            document.querySelector("#creatingPlayerName ~ .invalid-feedback")!.innerHTML = errorMessage;
            document.getElementById("creatingPlayerName")!.classList.add("is-invalid");
            return;
        }
        
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
                    modalProvider.show({ title: "Fehler bei der Anfrage", contentText: `${response.status}: ${response.statusText}` });
            });
    }

    function onJoinGame() {
        for (const element of document.getElementsByClassName("form-control"))
            element.classList.remove("is-invalid");

        const errorMessage = getPlayerNameError();
        if (errorMessage !== null) {
            document.querySelector("#joinPlayerName ~ .invalid-feedback")!.innerHTML = errorMessage;
            document.getElementById("joinPlayerName")!.classList.add("is-invalid");
            return;
        }
        
        const request: JoinGameDto = {
            playerName: playerName,
            gamePassword: (passwordRequired ? password : null)
        };
        apiClient.api.gameSessionsPlayersCreate(gameId ?? -1, request)
            .then(response => joinGame(response.data))
            .catch((response: HttpResponse<JoinedGameDto>) => {
                switch (response.status) {
                    case 400:
                        document.querySelector("#joinPlayerName ~ .invalid-feedback")!.innerHTML = "Der gewählte Name ist ungültig oder bereits vergeben."
                        document.getElementById("joinPlayerName")!.classList.add("is-invalid");
                        break;
                    case 401:
                        document.getElementById("joinPassword")!.classList.add("is-invalid");
                        break;
                    case 404:
                        document.getElementById("joinGameId")!.classList.add("is-invalid");
                        break;
                    default:
                        modalProvider.show({ title: "Fehler bei der Anfrage", contentText: `${response.status}: ${response.statusText}` });
                        break;
                }
            });
    }
    
    function joinGame(data: JoinedGameDto) {
        storePlayerToken(data.game.id!, data.self.id!, data.bearerToken!);
        goto(routes.game(data.game.id!, data.self.id!));
    }
    
    function getPlayerNameError(): string | null {
        const serverConfig = getServerConfig();
        
        if (playerName.trim().length === 0)
            return "Der Name darf nicht leer sein";
        if (playerName.includes(' '))
            return "Der Name darf keine Leerzeichen enthalten";
        if (playerName.length < serverConfig.playerNameMinLength! || playerName.length > serverConfig.playerNameMaxLength!)
            return `Der Name muss zwischen ${serverConfig.playerNameMinLength} und ${serverConfig.playerNameMaxLength} Zeichen lang sein`;
        if (!serverConfig.playerNameAllowNumbers && playerName.match("\d"))
            return "Es sind keine Zahlen im Namen erlaubt"
        
        return null;
    }
    
    let pollId: NodeJS.Timeout;
    onMount(() => {
        if (page.url.searchParams.get("kicked") !== null) {
            modalProvider.show({ title: "Spiel verlassen", contentText: "Sie wurden vom Game master aus dem Spiel geworfen." });
        }
        
        if (page.url.searchParams.get("gameId") !== null) {
            const urlGameId = Number.parseInt(page.url.searchParams.get("gameId")!);
            apiClient.api.gameSessionsDetail(urlGameId)
                .then(response => {
                    const session = getPlayerToken(response.data.id!);
                    if (!config.allowMultipleSessions && session !== undefined) {     // When only one session is allowed, rejoin the existing one
                        goto(routes.game(session.sessionId, session.playerId));
                        return;
                    }
                    
                    if (!response.data.canJoin) {
                        if ((response.data.playerCount ?? 0) >= (response.data.maxPlayerCount ?? 0))
                            modalProvider.show({ title: "Spiel beitritt nicht möglich", contentText: "Das Spiel hat bereits die maximale Anzahl an Spielern erreicht!" });
                        else
                            modalProvider.show({ title: "Spiel beitritt nicht möglich", contentText: "Das Spiel läuft bereits!" });
                        return;
                    }
                    
                    gameId = response.data.id;
                    gameIdLocked = true;
                    password = "";
                    passwordRequired = response.data.protected ?? true;

                    modalProvider.show({ title: "Spiel beitreten", content: joinModalContent, confirmText: "Beitreten", onConfirm: onJoinGame });
                })
                .catch(response => {
                    if (response.status === 404)
                        return;
                    modalProvider.show({ title: "Fehler bei der Anfrage", contentText: `${response.status}: ${response.statusText}` });
                });
        }

        if (getServerConfig().sessionsVisible) {
            apiClient.api.gameSessionsList()
                .then(response => {
                    games = response.data;
                    pollId = setInterval(async () => {
                        const response : HttpResponse<GameDto[], void> = await apiClient.api.gameSessionsList();
                        if (response.ok)
                            games = response.data;
                        else
                            console.error("Failed to poll game sessions!")
                    }, config.apiPollInterval);
                })
                .catch(err => {
                    games = null;
                    console.warn("Can't poll game session because its disabled by the server (although its enabled for the client)", err);
                });
        }
        else {
            games = null;
        }
        
        // Remove not non-existent sessions
        getPlayerTokens().forEach(session => {
            apiClient.api.gameSessionsDetail(session.sessionId)
                .then(response => {
                    if (!response.ok)
                        removePlayerToken(session.sessionId); 
                })
                .catch(error => {
                    if (error.status === 404)
                        removePlayerToken(session.sessionId);
                });
        });
        return () => clearInterval(pollId);
    });
    
    function formKeyDown(event: KeyboardEvent, submitCallback: () => void) {
        if (event.key === "Enter") {
            event.preventDefault();
            submitCallback();
        }
    }
</script>

{#snippet createModalContent()}
    <div class="mb-3">
        <label class="form-label" for="creatingPlayerName">Spielername</label>
        <input class="form-control" id="creatingPlayerName" type="text" bind:value={playerName} onkeydown={e => formKeyDown(e, onCreateGame)}/>
        <div class="invalid-feedback"></div>
    </div>

    <div class="mb-3">
        <label class="form-label" for="creatingPassword">Passwort</label>
        <input class="form-control" id="creatingPassword" type="password" bind:value={password}
               aria-describedby="creatingPasswordHelp" onkeydown={e => formKeyDown(e, onCreateGame)} />
        <div class="form-text" id="creatingPasswordHelp">
            Beitretende Spieler müssen dieses Passwort zum Beitreten eingeben. Passwort leer lassen um öffentliches Spiel zu erstellen.
        </div>
    </div>
{/snippet}

{#snippet joinModalContent()}
    <div class="mb-3">
        <label class="form-label" for="joinGameId">Spielcode</label>
        {#if gameIdLocked}
            <input class="form-control" id="joinGameId" type="number" value="{gameId}"
                   onkeydown={e => formKeyDown(e, onJoinGame)} disabled readonly />
        {:else}
            <input class="form-control" id="joinGameId" type="number" bind:value={gameId}
                   min="0" max="999999" onkeydown={e => formKeyDown(e, onJoinGame)} />
        {/if}
        <div class="invalid-feedback">Keine Spiele-Sitzung mit diesem Code konnte gefunden werden.</div>
    </div>

    <div class="mb-3">
        <label class="form-label" for="joinPlayerName">Spielername</label>
        <input class="form-control" id="joinPlayerName" type="text" bind:value={playerName} onkeydown={e => formKeyDown(e, onJoinGame)} />
        <div class="invalid-feedback"></div>
    </div>

    {#if passwordRequired}
        <div class="mb-3">
            <label class="form-label" for="joinPassword">Passwort</label>
            <input class="form-control" id="joinPassword" type="password" bind:value={password} onkeydown={e => formKeyDown(e, onJoinGame)} />
            <div class="invalid-feedback">Das angegebene Passwort ist nicht korrekt!</div>
        </div>
    {/if}
{/snippet}

<PageTitle title="Werwolf - Lobbies" />

<div class="row d-flex flex-wrap">
    <Button class="col start-button m-1" onclick={() => {
            password = "";
            modalProvider.show({ title: "Neues Spiel erstellen", content: createModalContent, confirmText: "Erstellen", onConfirm: onCreateGame });
    }}>Neues Spiel erstellen</Button>
    
    <Button variant="secondary" class="col start-button m-1" onclick={() => {
            gameId = undefined;
            gameIdLocked = false;
            password = "";
            passwordRequired = true;
            
            modalProvider.show({ title: "Spiel beitreten", content: joinModalContent, confirmText: "Beitreten", onConfirm: onJoinGame });
    }}>Spiel beitreten</Button>
</div>
<hr />

<h5>Vorhandene Spiele:</h5>
{#if games !== null}
    {#if games?.length > 0}
        <div class="d-flex flex-wrap">
            {#each games as game}
                <GameCard {game} onJoin={() => {
                    const session = getPlayerToken(game.id!);
                    if (!config.allowMultipleSessions && session !== undefined) {     // When only one session is allowed, rejoin the existing one
                        goto(routes.game(session.sessionId, session.playerId));
                        return;
                    }
                    
                    gameId = game.id;
                    gameIdLocked = true;
                    password = "";
                    passwordRequired = game.protected ?? true;

                    modalProvider.show({ title: "Spiel beitreten", content: joinModalContent, confirmText: "Beitreten", onConfirm: onJoinGame });
                    }} />
            {/each}
        </div>
    {:else}
        <h6>Es gibt noch keine Spiele</h6>
    {/if}
{:else}
    <h6>Vorhandene Spiele werden aufgrund der Serverkonfiguration nicht angezeigt.</h6>
{/if}

<style>
    :global(.start-button) {
        min-width: 16rem;
    }
</style>