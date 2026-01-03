using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace WerwolfDotnet.Server;

public static class Extensions
{
    extension(ClaimsPrincipal user)
    {
        public int GetGameId() => int.Parse(user.FindFirstValue(Claims.SessionId)!);
        
        public int GetPlayerId() => int.Parse(user.FindFirstValue(Claims.PlayerId)!);
        
        public string GetPlayerName() => user.FindFirstValue(Claims.PlayerName)!;
    }

    extension<T>(IHubClients<T> clients)
    {
        public T Player(int gameId, int playerId) => clients.User($"{gameId}:{playerId}");

        public T Players(int gameId, IEnumerable<int> playerIds) =>
            clients.Users(playerIds.Select(id => $"{gameId}:{id}"));
    }
}