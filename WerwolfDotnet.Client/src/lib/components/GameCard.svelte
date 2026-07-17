<script lang="ts">
    import { type GameDto } from "../../Api";
    import { config } from "../../config";
    import { getPlayerToken } from "../../stores/gameSessionStore";
    
    let { game, onJoin }: { game: GameDto, onJoin: (() => void)} = $props();
    let canRejoin = $derived(!config.allowMultipleSessions && getPlayerToken(game.id!) !== undefined);
    let clickable = $derived(game.canJoin || canRejoin)
</script>

<div class="card m-2 game-card">
    <div class="card-body">
        <h5 class="card-title">{(game.id ?? 0).toString().padStart(6, '0')}</h5>
        <p class="card-text">
            Game Master: {game.gameMaster}<br>
            Spieler: {game.playerCount}/{game.maxPlayerCount}
        </p>
        <button class="btn {!clickable ? 'btn-outline-danger' : 'btn-success'}" disabled={clickable === false} type="button" onclick={onJoin}>
            {#if canRejoin}
                wieder beitreten
            {:else if (game.playerCount ?? 0) >= (game.maxPlayerCount ?? 0)}
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