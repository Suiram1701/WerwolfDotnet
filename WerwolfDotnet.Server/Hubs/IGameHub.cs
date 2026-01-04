using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server.Hubs;

public interface IGameHub
{
    [HubMethodName("onGameMetaUpdated")]
    public Task GameMetaUpdated(GameMetadataDto metadata);
    
    [HubMethodName("onPlayersUpdated")]
    public Task PlayersUpdated(IEnumerable<PlayerDto> players);
    
    [HubMethodName("onGameStateUpdated")]
    public Task GameStateUpdated(GameStateDto state);
    
    [HubMethodName("onForceDisconnect")]
    public Task ForceDisconnect(bool kicked = false);
}