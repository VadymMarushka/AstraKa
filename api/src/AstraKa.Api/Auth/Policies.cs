namespace AstraKa.Api.Auth;

/// <summary>Policy names in one place, referenced everywhere instead of literal strings.</summary>
internal static class Policies
{
    public const string Wanderer = nameof(Wanderer);
    public const string Starshepherd = nameof(Starshepherd);
    public const string Oracle = nameof(Oracle);
    public const string AuthorOrOracle = nameof(AuthorOrOracle);
}
