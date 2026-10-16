namespace AstraKa.Application.Common;

/// <summary>
/// Who is making the current request. Implemented by Api - a temporary development stub for now
/// (<c>DevelopmentCurrentUser</c>), a real OIDC-backed implementation once lab 3's auth lands.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
