<script lang="ts">
    import { onMount } from "svelte";
    import { config } from "../config";
    import { Api, type HttpResponse, type GameDto } from "../Api";
    import PageTitle from "./components/PageTitle.svelte"
    import GameCard from "./components/GameCard.svelte";
    import CreateGameModal from "./components/modals/CreateGameModal.svelte";
    import JoinGameModal from "./components/modals/JoinGameModal.svelte";
    
    const apiClient = new Api({ baseUrl: config.apiEndpoint });
    let games: GameDto[] | null = $state(null);
    let selectedGame: GameDto | null = $state(null);

    let createModal: any = undefined;
    let joinModal: any = undefined;
    onMount(async () => {
        // @ts-ignore (bootstrap imported from CDN)
        joinModal = new bootstrap.Modal("#joinModal");
        // @ts-ignore (bootstrap imported from CDN)
        createModal = new bootstrap.Modal("#createModal");
        
        if (config.sessionsVisible) {
            games = [];
            const response : HttpResponse<GameDto[], void> = await apiClient.api.gameSessionsList();
            if (response.ok) {
                games = response.data;
                
                setInterval(async () => {
                    const response : HttpResponse<GameDto[], void> = await apiClient.api.gameSessionsList();
                    if (response.ok)
                        games = response.data;
                    else
                        console.error("Failed to poll game sessions!")
                }, config.sessionsPollInterval * 1000);     // s to ms
            }
            else {
                games = null;
                console.warn("Can't poll game session because its disabled by the server (altrough its enabled for the client)");
            }
        } 
    });
</script>

<PageTitle>Werwolf - Lobbies</PageTitle>

<CreateGameModal id="createModal" {apiClient} />
<JoinGameModal id="joinModal" preSetGame={selectedGame} {apiClient} />

<div class="d-flex flex-column justify-content-center">
    <div class="row d-flex flex-wrap">
        <button class="col btn btn-primary m-1 start-button" type="button" onclick={() => createModal.show()}>Neues Spiel erstellen</button>
        <button class="col btn btn-secondary m-1 start-button" type="button" onclick={() => {
            selectedGame = null;
            joinModal.show();
        }}>Spiel beitreten</button>
    </div>
    <hr />

    <h5>Vorhandene Spiele:</h5>
    {#if games !== null}
        {#if games?.length > 0}
            <div class="d-flex flex-wrap">
                {#each games as game}
                    <GameCard {game} onjoin={() => {
                        selectedGame = game;
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