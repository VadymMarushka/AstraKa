using System.Collections.Concurrent;
using AstraKa.Application.Horoscopes;

namespace AstraKa.Tests.Common;

/// <summary>
/// Test-only double for <see cref="IHoroscopeProvider"/> - counts calls per sign (to prove caching
/// works) and can simulate a failure for a specific sign (returning the exact fallback text, same
/// as the real provider's own contract - never throws). Each test should dedicate its own,
/// never-reused sign rather than resetting shared state, since this instance is shared across the
/// whole "Database" collection.
/// </summary>
public sealed class CountingHoroscopeProvider : IHoroscopeProvider
{
    private readonly ConcurrentDictionary<string, int> _callCounts = new();
    private readonly ConcurrentDictionary<string, bool> _failingSigns = new();

    public int CallCountFor(string signName) => _callCounts.GetValueOrDefault(signName.ToLowerInvariant());

    public void FailNextCallsFor(string signName) => _failingSigns[signName.ToLowerInvariant()] = true;

    public Task<string> GetDailyHoroscopeAsync(string signName, CancellationToken cancellationToken)
    {
        var key = signName.ToLowerInvariant();
        _callCounts.AddOrUpdate(key, 1, (_, count) => count + 1);

        return Task.FromResult(_failingSigns.ContainsKey(key) ? HoroscopeFallback.Text : $"The stars favor {signName} today.");
    }
}
