using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AstraKa.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Api.Auth;

/// <summary>
/// Looks up the token's `sub` claim against <c>Users.ExternalId</c> and, if found, adds the
/// internal <c>UserId</c>/<c>Role</c> claims so nothing downstream (<see cref="ICurrentUser"/>,
/// policies) ever hits the database again this request. No match means the caller is authenticated
/// but hasn't onboarded yet - a real state, not an error, so the principal is returned unchanged.
/// </summary>
internal sealed class DatabaseClaimsTransformation(IAppDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        // Idempotent: claims transformation can run more than once per request.
        if (principal.HasClaim(c => c.Type == AstraKaClaimTypes.UserId))
        {
            return principal;
        }

        var externalId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(externalId))
        {
            return principal;
        }

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.ExternalId == externalId);
        if (user is null)
        {
            return principal;
        }

        identity.AddClaim(new Claim(AstraKaClaimTypes.UserId, user.Id.ToString()));
        identity.AddClaim(new Claim(AstraKaClaimTypes.Role, user.Role.ToString()));

        return principal;
    }
}
