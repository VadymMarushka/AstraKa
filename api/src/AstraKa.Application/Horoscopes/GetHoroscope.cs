using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace AstraKa.Application.Horoscopes;

/// <summary>Today's horoscope for a zodiac sign - no visibility/authorization, anyone can ask for any sign.</summary>
public sealed record GetHoroscopeQuery(int ZodiacSignId) : IRequest<HoroscopeDto>;

public sealed record HoroscopeDto(int ZodiacSignId, string SignName, DateOnly Date, string Horoscope);

public sealed class GetHoroscopeQueryHandler(
    IAppDbContext db,
    IHoroscopeProvider provider,
    HybridCache cache,
    TimeProvider timeProvider,
    IOptions<HoroscopeOptions> options) : IRequestHandler<GetHoroscopeQuery, HoroscopeDto>
{
    public async Task<HoroscopeDto> Handle(GetHoroscopeQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (!options.Value.CacheEnabled)
        {
            return await FetchAsync(request.ZodiacSignId, today, cancellationToken);
        }

        var cacheKey = $"horoscope:{request.ZodiacSignId}:{today:yyyy-MM-dd}";
        var midnightUtc = new DateTimeOffset(today.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var fullDayTtl = midnightUtc - timeProvider.GetUtcNow();

        var dto = await cache.GetOrCreateAsync(
            cacheKey,
            async ct => await FetchAsync(request.ZodiacSignId, today, ct),
            new HybridCacheEntryOptions { Expiration = fullDayTtl, LocalCacheExpiration = fullDayTtl },
            cancellationToken: cancellationToken);

        if (dto.Horoscope == HoroscopeFallback.Text)
        {
            // Don't let a transient provider failure poison the cache until UTC midnight - shrink
            // this entry's TTL so the next request tries the provider again soon instead.
            var shortTtl = TimeSpan.FromSeconds(30);
            await cache.SetAsync(cacheKey, dto, new HybridCacheEntryOptions { Expiration = shortTtl, LocalCacheExpiration = shortTtl }, cancellationToken: cancellationToken);
        }

        return dto;
    }

    private async Task<HoroscopeDto> FetchAsync(int signId, DateOnly today, CancellationToken ct)
    {
        // Runs inside the cache factory on a miss only - a cache hit never reaches this method,
        // so an unknown signId throwing here (instead of being checked before GetOrCreateAsync)
        // is exactly what keeps a cache hit at zero I/O. HybridCache never caches a factory
        // exception, so a bad id correctly 404s every time rather than being cached as "missing".
        var sign = await db.ZodiacSigns.AsNoTracking().SingleOrDefaultAsync(z => z.Id == signId, ct);
        if (sign is null)
        {
            throw new NotFoundException($"Zodiac sign {signId} does not exist.");
        }

        var text = await provider.GetDailyHoroscopeAsync(sign.Name, ct);
        return new HoroscopeDto(sign.Id, sign.Name, today, text);
    }
}
