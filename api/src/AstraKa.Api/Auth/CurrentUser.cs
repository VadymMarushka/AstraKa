using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AstraKa.Application.Common;
using AstraKa.Domain.Enums;

namespace AstraKa.Api.Auth;

/// <summary>
/// Reads <see cref="IHttpContextAccessor.HttpContext"/>'s <c>User</c> claims only - everything it
/// needs is already there, either straight from the validated JWT (`sub`, the Auth0 email custom
/// claim) or added once per request by <see cref="DatabaseClaimsTransformation"/>.
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? ExternalId => Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

    public string? Email => Principal?.FindFirstValue(AstraKaClaimTypes.Email);

    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(AstraKaClaimTypes.UserId), out var id) ? id : null;

    public UserRole? Role => Enum.TryParse<UserRole>(Principal?.FindFirstValue(AstraKaClaimTypes.Role), out var role) ? role : null;
}
