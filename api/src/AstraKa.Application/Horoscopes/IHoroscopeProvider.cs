namespace AstraKa.Application.Horoscopes;

/// <summary>
/// A daily horoscope source. Implemented in Infrastructure (a real HTTP client against
/// freehoroscopeapi.com, or a fake with artificial latency for load testing - selected by
/// config). Never throws on an ordinary failure - the one exception is the caller's own
/// cancellation, which still propagates (see <c>HoroscopeProvider</c>'s doc comment).
/// </summary>
public interface IHoroscopeProvider
{
    Task<string> GetDailyHoroscopeAsync(string signName, CancellationToken cancellationToken);
}

/// <summary>The lore text shown when the real provider is unreachable, times out, or returns something unusable.</summary>
public static class HoroscopeFallback
{
    public const string Text = "The stars are silent today.";
}
