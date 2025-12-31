using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server.Hubs;

public interface IGameHub
{
    [HubMethodName("onPlayersUpdated")]
    public Task PlayersUpdated(IEnumerable<PlayerDto> players);
}