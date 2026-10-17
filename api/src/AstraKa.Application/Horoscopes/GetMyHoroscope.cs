using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace AstraKa.Application.Horoscopes;

/// <summary>Today's horoscope for the signed-in Wanderer's own sign. No visibility check beyond the Wanderer policy - that's the Api's job.</summary>
public sealed record GetMyHoroscopeQuery : IRequest<HoroscopeDto>;

public sealed class GetMyHoroscopeQueryHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IHoroscopeProvider provider,
    HybridCache cache,
    TimeProvider timeProvider,
    IOptions<HoroscopeOptions> options) : IRequestHandler<GetMyHoroscopeQuery, HoroscopeDto>
{
    public async Task<HoroscopeDto> Handle(GetMyHoroscopeQuery request, CancellationToken cancellationToken)
    {
        // Safe: this query is only ever reached once the Wanderer policy has already passed, so
        // the caller is guaranteed to be an onboarded Wanderer with a real ZodiacSignId.
        var zodiacSignId = await db.Wanderers
            .Where(w => w.UserId == currentUser.UserId!.Value)
            .Select(w => w.ZodiacSignId)
            .SingleAsync(cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (!options.Value.CacheEnabled)
        {
            return await FetchAsync(zodiacSignId, today, cancellationToken);
        }

        // Same cache key scheme as GetHoroscopeQueryHandler - the two endpoints share one entry
        // for the same sign/day.
        var cacheKey = $"horoscope:{zodiacSignId}:{today:yyyy-MM-dd}";
        var midnightUtc = new DateTimeOffset(today.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var fullDayTtl = midnightUtc - timeProvider.GetUtcNow();

        var dto = await cache.GetOrCreateAsync(
            cacheKey,
            async ct => await FetchAsync(zodiacSignId, today, ct),
            new HybridCacheEntryOptions { Expiration = fullDayTtl, LocalCacheExpiration = fullDayTtl },
            cancellationToken: cancellationToken);

        if (dto.Horoscope == HoroscopeFallback.Text)
        {
            var shortTtl = TimeSpan.FromSeconds(30);
            await cache.SetAsync(cacheKey, dto, new HybridCacheEntryOptions { Expiration = shortTtl, LocalCacheExpiration = shortTtl }, cancellationToken: cancellationToken);
        }

        return dto;
    }

    private async Task<HoroscopeDto> FetchAsync(int signId, DateOnly today, CancellationToken ct)
    {
        var sign = await db.ZodiacSigns.AsNoTracking().SingleOrDefaultAsync(z => z.Id == signId, ct);
        if (sign is null)
        {
            throw new NotFoundException($"Zodiac sign {signId} does not exist.");
        }

        var text = await provider.GetDailyHoroscopeAsync(sign.Name, ct);
        return new HoroscopeDto(sign.Id, sign.Name, today, text);
    }
}
