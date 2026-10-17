namespace AstraKa.Application.Horoscopes;

/// <summary>Bound from the "Horoscope" config section.</summary>
public sealed class HoroscopeOptions
{
    /// <summary>The "before/after" load-test switch - no code change needed to flip it.</summary>
    public bool CacheEnabled { get; set; } = true;

    /// <summary>"Real" (the actual freehoroscopeapi.com client) or "Fake" (artificial latency, for load testing - never the real third-party API).</summary>
    public string Provider { get; set; } = "Real";

    public int FakeLatencyMs { get; set; } = 300;
}
