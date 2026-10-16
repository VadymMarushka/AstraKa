using AstraKa.Domain.Enums;

namespace AstraKa.Application.Common;

/// <summary>
/// Who is making the current request. Implemented by Api's <c>CurrentUser</c>, reading claims from
/// the validated Auth0 JWT plus whatever <c>DatabaseClaimsTransformation</c> added. A signed-in
/// user who hasn't onboarded yet has <see cref="ExternalId"/> but no <see cref="UserId"/>/
/// <see cref="Role"/> - that's a real, first-class state, not an error.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The token's `sub` claim.</summary>
    string? ExternalId { get; }

    /// <summary>The Auth0 custom claim added by the tenant's Post Login Action.</summary>
    string? Email { get; }

    /// <summary>Our <c>User.Id</c> - null until this account has onboarded.</summary>
    Guid? UserId { get; }

    /// <summary>Null until this account has onboarded.</summary>
    UserRole? Role { get; }
}
