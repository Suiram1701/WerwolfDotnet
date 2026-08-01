using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace WerwolfDotnet.Server.Authentication;

public class AdminAuthenticationScheme(
    IOptionsMonitor<AdminAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AdminAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "BasicAdmin";
    
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.Enabled || Options.Users.Count == 0)
            return Task.FromResult(AuthenticateResult.NoResult());
        
        AuthenticationHeaderValue.TryParse(Context.Request.Headers.Authorization, out AuthenticationHeaderValue? headerValue);
        if (headerValue?.Scheme != "Basic")
            return Task.FromResult(AuthenticateResult.NoResult());
        if (!Base64.IsValid(headerValue.Parameter))
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));

        string decodedValue = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue.Parameter!));
        string[] parts = decodedValue.Split(':', 2);
        if (!Options.Users.TryGetValue(parts[0], out string? password) || parts[1] != password)
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password!"));
        
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims: [
            new Claim(Claims.SessionId, "-1"),
            new Claim(Claims.PlayerId, "-1"),
            new Claim(Claims.PlayerName, "Server-ADM"),
            new Claim(Claims.IsAdmin, "true")
        ], authenticationType: SchemeName, nameType: Claims.PlayerName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, null, SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"Admin access\"";
        return base.HandleChallengeAsync(properties);
    }
}