using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services;

namespace WerwolfDotnet.Server.Hubs;

public interface IGameHub
{
    [HubMethodName("onGameMetaUpdated")]
    public Task GameMetaUpdated(int gameMasterId, int? mayorId);
    
    /// <remarks>
    /// NOTE: Do not use directly as soon as the game started!
    /// </remarks>
    [HubMethodName("onPlayersUpdated")]
    public Task PlayersUpdated(IEnumerable<PlayerDto> players);
    
    [HubMethodName("onGameStateUpdated")]
    public Task GameStateUpdated(GameState state, IReadOnlyDictionary<int, DeathDetails> diedPlayers);

    /// <remarks>
    /// NOTE: Do not use directly! Use <see cref="GameManager.UpdatePlayerRoleAsync"/> instead.
    /// </remarks>
    [HubMethodName("onPlayerRoleUpdated")]
    public Task PlayerRoleUpdated(Role role, IReadOnlyDictionary<int, PlayerRelation[]> selfRelations);
    
    [HubMethodName("onActionRequested")]
    public Task PlayerActionRequested(SelectionOptionsDto options);

    [HubMethodName("onVotesUpdated")]
    public Task VotesUpdated(IReadOnlyDictionary<int, int[]> votes);
    
    [HubMethodName("onActionCompleted")]
    public Task PlayerActionCompleted(string[]? parameters);

    [HubMethodName("onGameWon")]
    public Task GameWon(Fraction fraction);
    
    [HubMethodName("onForceDisconnect")]
    public Task ForceDisconnect(bool kicked = false);
}

public record DeathDetails(CauseOfDeath Cause, Role Role);     // Required because SignalR doesn't transfer tuples