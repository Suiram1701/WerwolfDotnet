using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Models;

namespace WerwolfDotnet.Server;

public static class Extensions
{
    public static IEnumerable<PlayerDto> ToDtoCollection(this IEnumerable<Player> players) =>
        players.Select(p => new PlayerDto(p));
    
    extension(ClaimsPrincipal user)
    {
        public int GetGameId() => int.Parse(user.FindFirstValue(Claims.SessionId)!);
        
        public int GetPlayerId() => int.Parse(user.FindFirstValue(Claims.PlayerId)!);
        
        public string GetPlayerName() => user.FindFirstValue(Claims.PlayerName)!;
    }

    extension<T>(IHubClients<T> clients)
    {
        public T Game(int gameId) => clients.Group(GroupNames.Game(gameId));
        
        public T Player(int gameId, int playerId) => clients.User($"{gameId}:{playerId}");

        public T Players(int gameId, IEnumerable<int> playerIds) =>
            clients.Users(playerIds.Select(id => $"{gameId}:{id}"));
    }
}