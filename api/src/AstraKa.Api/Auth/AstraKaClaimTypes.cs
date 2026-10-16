namespace AstraKa.Api.Auth;

/// <summary>
/// Every claim type this app cares about beyond the token's own `sub`, named in one place.
/// </summary>
internal static class AstraKaClaimTypes
{
    /// <summary>Added by <see cref="DatabaseClaimsTransformation"/> once a matching User row is found.</summary>
    public const string UserId = "astraka:user_id";

    /// <summary>Added by <see cref="DatabaseClaimsTransformation"/> once a matching User row is found.</summary>
    public const string Role = "astraka:role";

    /// <summary>
    /// The Auth0 custom claim URI set by the tenant's Post Login Action - access tokens don't carry
    /// `email` by default. Read as-is; this app never adds it itself.
    /// </summary>
    public const string Email = "https://astraka.app/email";
}
