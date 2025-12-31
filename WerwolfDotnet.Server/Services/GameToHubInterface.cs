using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Hubs;
using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server.Services;

/// <summary>
/// A service which provides a communication layer from game to clients using events.
/// </summary>
/// <param name="manager"></param>
/// <param name="hubContext"></param>
public sealed class GameToHubInterface(GameManager manager, IHubContext<GameHub, IGameHub> hubContext) : IHostedService
{
    private readonly GameManager _manager = manager;
    private readonly IHubContext<GameHub, IGameHub> _hubContext = hubContext;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _manager.OnPlayersUpdated += PlayersUpdated;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _manager.OnPlayersUpdated -= PlayersUpdated;
        return Task.CompletedTask;
    }
    
    private Task PlayersUpdated(GameContext ctx, IEnumerable<Player> players)
    {
        IEnumerable<PlayerDto> newPlayers = players.Select(p => new PlayerDto(p));
        return _hubContext.Clients.Group(GroupNames.Game(ctx.Id)).PlayersUpdated(newPlayers);
    }
}