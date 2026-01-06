using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Hubs;

namespace WerwolfDotnet.Server.Services;

/// <summary>
/// A service which provides a communication layer from game to clients using events.
/// </summary>
public sealed class PlayerConnectionMapper(GameManager manager, IHubContext<GameHub, IGameHub> hubContext)
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
}