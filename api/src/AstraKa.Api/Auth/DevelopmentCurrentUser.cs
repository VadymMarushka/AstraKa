using AstraKa.Application.Common;

namespace AstraKa.Api.Auth;

/// <summary>
/// Temporary stand-in for <see cref="ICurrentUser"/> until lab 3's real OIDC authentication
/// replaces it. Always reports "nobody signed in" - nothing in this project reads
/// <see cref="ICurrentUser"/> for an actual decision yet.
/// </summary>
internal sealed class DevelopmentCurrentUser : ICurrentUser
{
    public Guid? UserId => null;

    public bool IsAuthenticated => false;
}
