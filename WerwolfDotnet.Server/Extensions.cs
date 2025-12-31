using System.Security.Claims;

namespace WerwolfDotnet.Server;

public static class Extensions
{
    public static int GetGameId(this ClaimsPrincipal user) => int.Parse(user.FindFirstValue(Claims.SessionId)!);

    public static int GetPlayerId(this ClaimsPrincipal user) => int.Parse(user.FindFirstValue(Claims.PlayerId)!);

    public static string GetPlayerName(this ClaimsPrincipal user) => user.FindFirstValue(Claims.PlayerName)!;
}