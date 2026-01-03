using Microsoft.AspNetCore.SignalR;

namespace WerwolfDotnet.Server.Services;

public sealed class TokenAuthenticationUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        if (!(connection.User.Identity?.IsAuthenticated ?? false))
            return null;
        return $"{connection.User.GetGameId()}:{connection.User.GetPlayerId()}";
    }
}