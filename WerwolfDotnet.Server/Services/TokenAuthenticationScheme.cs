using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using WerwolfDotnet.Server.Game;

namespace WerwolfDotnet.Server.Services;

public class TokenAuthenticationScheme(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    GameManager manager,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly GameManager _manager = manager;

    public const string SchemeName = "PlayerToken";
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Context.Request.Headers.Authorization, out AuthenticationHeaderValue? headerValue)
            || headerValue.Scheme != SchemeName)
            return AuthenticateResult.NoResult();
        
        string? sessionIdStr = Context.Request.Query[GameSessionIdParam];
        string? playerIdStr = Context.Request.Query[PlayerIdParam];
        if (string.IsNullOrEmpty(sessionIdStr) || string.IsNullOrEmpty(playerIdStr))
            return AuthenticateResult.NoResult();
        
        if (!int.TryParse(sessionIdStr, out int sessionId) || !int.TryParse(playerIdStr, out int playerId))
            return AuthenticateResult.Fail("A valid sessionId and playerId was expected.");

        GameContext? context = await _manager.GetGameById(sessionId).ConfigureAwait(false);
        Player? player = context?.Players.SingleOrDefault(p => p!.Id == playerId, null);
        if (context is null || player is null)
            return AuthenticateResult.Fail("Game or player not found.");

        if (player.VerifyAuthToken(headerValue.Parameter ?? string.Empty))
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims: [
                new Claim(Claims.SessionId, context.Id.ToString()),
                new Claim(Claims.PlayerId, player.Id.ToString()),
                new Claim(Claims.PlayerName, player.Name),
            ], authenticationType: SchemeName, nameType: Claims.PlayerName));
            return AuthenticateResult.Success(new AuthenticationTicket(principal, null, SchemeName));
        }
        return AuthenticateResult.Fail("Invalid token.");
    }
}