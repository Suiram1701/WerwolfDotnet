<script lang="ts">
    import { type GameDto } from "../../Api";

    let { game, onJoin }: { game: GameDto, onJoin: (() => void)} = $props();
    let gameLocked : Boolean = $derived(game.isRunning || (game.playerCount ?? 0) >= (game.maxPlayerCount ?? 0));
</script>

<div class="card m-2 game-card">
    <div class="card-body">
        <h5 class="card-title">{(game.id ?? 0).toString().padStart(6, '0')}</h5>
        <p class="card-text">
            Game master: {game.gameMaster}<br>
            Spieler: {game.playerCount}/{game.maxPlayerCount}
        </p>
        <button class="btn {gameLocked ? 'btn-outline-danger' : 'btn-success'}" disabled={gameLocked === true} type="button" onclick={onJoin}>
            {#if game.isRunning}
                Spiel läuft bereits
            {:else if gameLocked}     <!--- Only matches when the session is full. game.isRunning is caught before --->
                Sitzung voll
            {:else}
                Beitreten
            {/if}
        </button>
    </div>
</div>

<style>
    .game-card {
        width: 18rem;
    }
</style>