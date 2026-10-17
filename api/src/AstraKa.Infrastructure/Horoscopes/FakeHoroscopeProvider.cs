using AstraKa.Application.Horoscopes;
using Microsoft.Extensions.Options;

namespace AstraKa.Infrastructure.Horoscopes;

/// <summary>
/// Stands in for the real provider when <c>Horoscope:Provider</c> is "Fake" - artificial latency,
/// no network call at all. Selected for k6 load testing so neither "before" nor "after" run ever
/// touches the real third-party API.
/// </summary>
internal sealed class FakeHoroscopeProvider(IOptions<HoroscopeOptions> options) : IHoroscopeProvider
{
    public async Task<string> GetDailyHoroscopeAsync(string signName, CancellationToken cancellationToken)
    {
        await Task.Delay(options.Value.FakeLatencyMs, cancellationToken);
        return $"The stars favor {signName} today.";
    }
}
