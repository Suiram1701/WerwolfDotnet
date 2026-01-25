using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server.Hubs;

public interface IGameHub
{
    [HubMethodName("onGameMetaUpdated")]
    public Task GameMetaUpdated(int gameMasterId, int? mayorId);
    
    [HubMethodName("onPlayersUpdated")]
    public Task PlayersUpdated(IEnumerable<PlayerDto> players);
    
    [HubMethodName("onGameStateUpdated")]
    public Task GameStateUpdated(GameState state, IEnumerable<int> diedPlayers);

    [HubMethodName("onPlayerRoleUpdated")]
    public Task PlayerRoleUpdated(Role role);
    
    [HubMethodName("onActionRequested")]
    public Task PlayerActionRequested(SelectionOptionsDto options);

    [HubMethodName("onVotesUpdated")]
    public Task VotesUpdated(IReadOnlyDictionary<int, int[]> votes);
    
    [HubMethodName("onActionCompleted")]
    public Task PlayerActionCompleted(string[]? parameters);
    
    [HubMethodName("onForceDisconnect")]
    public Task ForceDisconnect(bool kicked = false);
}