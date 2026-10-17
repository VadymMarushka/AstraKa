using System.Net.Http.Json;
using AstraKa.Application.Horoscopes;
using Microsoft.Extensions.Logging;

namespace AstraKa.Infrastructure.Horoscopes;

/// <summary>
/// The real provider - freehoroscopeapi.com, verified directly (no API key needed; response shape
/// is <c>{ data: { date, period, sign, horoscope } }</c>). Never throws on an ordinary failure
/// (timeout, non-success status, bad JSON) - returns <see cref="HoroscopeFallback"/> instead. The
/// one exception: a cancellation caused by the *caller's own* token still propagates, so a
/// disconnected client can never get the fallback cached for everyone else asking about this
/// sign/day.
/// </summary>
internal sealed class HoroscopeProvider(HttpClient httpClient, ILogger<HoroscopeProvider> logger) : IHoroscopeProvider
{
    public async Task<string> GetDailyHoroscopeAsync(string signName, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"api/v1/get-horoscope/daily?sign={Uri.EscapeDataString(signName.ToLowerInvariant())}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<HoroscopeResponse>(cancellationToken: cancellationToken);
            return payload?.Data?.Horoscope ?? HoroscopeFallback.Text;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller's own token fired - not a provider failure. Rethrowing (not returning the
            // fallback) means this never gets cached.
            throw;
        }
        catch (Exception ex)
        {
            // Everything else: HttpClient's own timeout (a TaskCanceledException where
            // cancellationToken itself is NOT the one that fired), HttpRequestException, JSON
            // parse failures - all genuinely "the provider failed", so: the fallback.
            logger.LogWarning(ex, "Horoscope provider failed for sign {SignName}; returning fallback.", signName);
            return HoroscopeFallback.Text;
        }
    }

    private sealed record HoroscopeResponse(HoroscopeData? Data);

    private sealed record HoroscopeData(string? Horoscope);
}
