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
        string? playerToken;
        if (AuthenticationHeaderValue.TryParse(Context.Request.Headers.Authorization,
                out AuthenticationHeaderValue? headerValue) && headerValue.Scheme is SchemeName or "Bearer") // Have to allow the Bearer scheme (although it isn't a bearer token) because browser WebSockets can only use this scheme.
            playerToken = headerValue.Parameter;
        else
            playerToken = Context.Request.Query["access_token"];     // SignalR uses this parameter when the browser doesn't allow custom headers
        
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

        if (player.VerifyAuthToken(playerToken ?? string.Empty))
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