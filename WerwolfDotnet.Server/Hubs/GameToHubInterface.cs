using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Models;
using WerwolfDotnet.Server.Services;

namespace WerwolfDotnet.Server.Hubs;

/// <summary>
/// A service which provides a communication layer from game to clients using events.
/// </summary>
public sealed class GameToHubInterface(GameManager manager, IHubContext<GameHub, IGameHub> hubContext) : IHostedService
{
    private readonly GameManager _manager = manager;
    private readonly IHubContext<GameHub, IGameHub> _hubContext = hubContext;

    private readonly Lock _mapLock = new();
    private readonly Dictionary<(int, int), ICollection<string>> _playerConnectionMapping = [];

    public void AddConnectionToPlayer(int gameId, int playerId, string connectionId)
    {
        lock (_mapLock)
        {
            if (_playerConnectionMapping.TryGetValue((gameId, playerId), out ICollection<string>? connections))
                connections.Add(connectionId);     // Updated through reference
            else
                _playerConnectionMapping.Add((gameId, playerId), [connectionId]);
        }
    }

    public string[] GetPlayerConnections(int gameId, int playerId)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        return _playerConnectionMapping.GetValueOrDefault((gameId, playerId))?.ToArray() ?? [];
    }

    public void RemoveConnectionFromPlayer(int gameId, int playerId, string connectionId)
    {
        lock (_mapLock)
        {
            if (!_playerConnectionMapping.TryGetValue((gameId, playerId), out ICollection<string>? connections))
                return;
            
            if (connections.Count > 1)
                connections.Remove(connectionId);     // Updated through reference
            else
                _playerConnectionMapping.Remove((gameId, playerId));
        }
    }
    
    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _manager.OnGameMetaUpdated += GameMetadataUpdated;
        _manager.OnPlayersUpdated += PlayersUpdated;
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _manager.OnGameMetaUpdated -= GameMetadataUpdated;
        _manager.OnPlayersUpdated -= PlayersUpdated;
        return Task.CompletedTask;
    }

    private Task GameMetadataUpdated(GameContext ctx, Player gameMaster, Player? mayor)
    {
        return _hubContext.Clients.Group(GroupNames.Game(ctx.Id)).GameMetaUpdated(new GameMetadataDto
        {
            GameMasterId = gameMaster.Id,
            MayorId = mayor?.Id
        });
    }
    
    private Task PlayersUpdated(GameContext ctx, IEnumerable<Player> players)
    {
        IEnumerable<PlayerDto> newPlayers = players.Select(p => new PlayerDto(p));
        return _hubContext.Clients.Group(GroupNames.Game(ctx.Id)).PlayersUpdated(newPlayers);
    }
}