<script lang="ts">
    import { type GameDto } from "../../Api";

    let { game, onjoin }: { game: GameDto, onjoin: (() => void)} = $props();
    let gameLocked : Boolean = $derived(game.isRunning || game.playerCount >= game.maxPlayerCount);
</script>

<div class="card m-2 game-card">
    <div class="card-body">
        <h5 class="card-title">{game.id.toString().padStart(6, '0')}</h5>
        <p class="card-text">
            Gamemaster: {game.gameMaster}<br>
            Spieler: {game.playerCount}/{game.maxPlayerCount}
        </p>
        <button class="btn {gameLocked ? 'btn-outline-danger' : 'btn-success'}" disabled={gameLocked == true} type="button" onclick={onjoin}>
            {#if game.isRunning}
                Spiel läuft bereits
            {:else if game.playerCount >= game.maxPlayerCount}
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