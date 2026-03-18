using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using WerwolfDotnet.Server.Services;

namespace WerwolfDotnet.Server.Authentication;

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
        AuthenticationHeaderValue.TryParse(Context.Request.Headers.Authorization, out AuthenticationHeaderValue? headerValue);
        if (!(headerValue?.Scheme is "Bearer" || Context.Request.Query.ContainsKey("access_token")))     // access_token is used by signalR
            return AuthenticateResult.NoResult();
        
        string? encodedToken = headerValue?.Parameter ?? Context.Request.Query["access_token"];
        if (!Base64.IsValid(encodedToken) || JsonSerializer.Deserialize<Token>(Convert.FromBase64String(encodedToken!)) is not { } token)
            return AuthenticateResult.Fail("Invalid token.");

        GameContext? context = await _manager.GetGameById(token.SessionId).ConfigureAwait(false);
        Player? player = context?.Players.SingleOrDefault(p => p!.Id == token.PlayerId, null);
        if (context is null || player is null)
            return AuthenticateResult.Fail("Game or player not found.");

        if (player.VerifyAuthToken(token.Auth))
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims: [
                new Claim(Claims.SessionId, context.Id.ToString()),
                new Claim(Claims.PlayerId, player.Id.ToString()),
                new Claim(Claims.PlayerName, player.Name),
            ], authenticationType: SchemeName, nameType: Claims.PlayerName));
            return AuthenticateResult.Success(new AuthenticationTicket(principal, null, SchemeName));
        }
        return AuthenticateResult.Fail("incorrect token.");
    }

    public record Token(int SessionId, int PlayerId, string Auth);
}