<script lang="ts">
    import { type GameDto } from "../../Api";

    let { game, onJoin }: { game: GameDto, onJoin: (() => void)} = $props();
</script>

<div class="card m-2 game-card">
    <div class="card-body">
        <h5 class="card-title">{(game.id ?? 0).toString().padStart(6, '0')}</h5>
        <p class="card-text">
            Game master: {game.gameMaster}<br>
            Spieler: {game.playerCount}/{game.maxPlayerCount}
        </p>
        <button class="btn {!game.canJoin ? 'btn-outline-danger' : 'btn-success'}" disabled={game.canJoin === false} type="button" onclick={onJoin}>
            {#if (game.playerCount ?? 0) >= (game.maxPlayerCount ?? 0)}
                Sitzung voll
            {:else if !game.canJoin}
                Beitritt gesperrt
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