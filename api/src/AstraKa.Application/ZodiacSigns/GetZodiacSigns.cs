using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.ZodiacSigns;

/// <summary>
/// The one sample query proving the MediatR + validation + exception-handling pipeline works end
/// to end. All 12 signs, no paging/filtering - this is reference data, not a feature endpoint.
/// </summary>
public sealed record GetZodiacSignsQuery : IRequest<IReadOnlyList<ZodiacSignDto>>;

public sealed record ZodiacSignDto(int Id, string Name, int SequenceNumber, int AzimuthDegrees);

public sealed class GetZodiacSignsQueryHandler(IAppDbContext db) : IRequestHandler<GetZodiacSignsQuery, IReadOnlyList<ZodiacSignDto>>
{
    public async Task<IReadOnlyList<ZodiacSignDto>> Handle(GetZodiacSignsQuery request, CancellationToken cancellationToken)
    {
        // AzimuthDegrees is a computed C# property with no backing column, so the signs are
        // materialized first and projected to the DTO afterward, in memory - a server-translated
        // .Select() can't reach into that property getter.
        var signs = await db.ZodiacSigns
            .AsNoTracking()
            .OrderBy(z => z.SequenceNumber)
            .ToListAsync(cancellationToken);

        return signs
            .Select(z => new ZodiacSignDto(z.Id, z.Name, z.SequenceNumber, z.AzimuthDegrees))
            .ToList();
    }
}
