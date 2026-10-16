using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AstraKa.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AstraKa.Tests.Auth;

/// <summary>
/// Stands in for real Auth0 validation in integration tests. Reads `X-Test-Sub`/`X-Test-Email`
/// request headers and produces a <see cref="ClaimsPrincipal"/> with those as `sub`/`email` claims
/// - no headers means no authentication result at all, so `[Authorize]` endpoints behave exactly as
/// they would with no bearer token (401).
/// </summary>
public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Sub", out var sub) || string.IsNullOrEmpty(sub))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, sub!) };
        if (Request.Headers.TryGetValue("X-Test-Email", out var email) && !string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(AstraKaClaimTypes.Email, email!));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
