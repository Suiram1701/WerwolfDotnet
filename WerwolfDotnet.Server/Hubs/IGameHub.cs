using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server.Hubs;

public interface IGameHub
{
    [HubMethodName("onGameMetaUpdated")]
    public Task GameMetaUpdated(GameMetadataDto metadata);
    
    [HubMethodName("onPlayersUpdated")]
    public Task PlayersUpdated(IEnumerable<PlayerDto> players);
    
    [HubMethodName("onGameStateUpdated")]
    public Task GameStateUpdated(GameState state, IEnumerable<int> diedPlayers);

    [HubMethodName("onPlayerRoleUpdated")]
    public Task PlayerRoleUpdated(string roleName);
    
    [HubMethodName("onActionRequested")]
    public Task PlayerActionRequested(ActionOptions options);
    
    [HubMethodName("onActionCompleted")]
    public Task PlayerActionCompleted(string? actionName, string[]? parameters);
    
    [HubMethodName("onForceDisconnect")]
    public Task ForceDisconnect(bool kicked = false);
}