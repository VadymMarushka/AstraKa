using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Reports;

/// <summary>
/// The remaining lab 2 LINQ item: an EF Core GroupBy report, not ADO.NET - per calendar date of
/// the cohort, average harmony level, number of reports, and number of completed exercises. No
/// visibility/authorization here - the controller resolves that via <c>GetCohortByIdQuery</c> first.
/// </summary>
public sealed record GetHarmonyTrendQuery(Guid CohortId) : IRequest<IReadOnlyList<HarmonyTrendPointDto>>;

public sealed record HarmonyTrendPointDto(DateOnly Date, double AverageHarmonyLevel, int ReportCount, int CompletedExerciseCount);

public sealed class GetHarmonyTrendQueryHandler(IAppDbContext db) : IRequestHandler<GetHarmonyTrendQuery, IReadOnlyList<HarmonyTrendPointDto>>
{
    public async Task<IReadOnlyList<HarmonyTrendPointDto>> Handle(GetHarmonyTrendQuery request, CancellationToken cancellationToken)
    {
        // EF Core cannot translate aggregating a joined/computed value *after* a GroupBy - only a
        // GroupBy+aggregate over the grouped rows' own native columns reliably translates. So this
        // is two independent GroupBy queries (each a single translated SQL statement, aggregating
        // only native columns) merged by date in memory - the grouping/aggregation itself stays
        // entirely server-side; only combining two already-small, already-aggregated result sets
        // happens in C#, which is not the client-side grouping CLAUDE.md's "no client-side
        // grouping" rules out.
        var reportStats = await db.DailyReports
            .AsNoTracking()
            .Where(r => r.CohortId == request.CohortId)
            .GroupBy(r => r.Date)
            .Select(g => new { Date = g.Key, Average = g.Average(r => (double)r.HarmonyLevel), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var completionCountsByDate = await db.ExerciseCompletions
            .AsNoTracking()
            .Where(c => c.DailyReport.CohortId == request.CohortId)
            .GroupBy(c => c.DailyReport.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

        return reportStats
            .Select(r => new HarmonyTrendPointDto(r.Date, r.Average, r.Count, completionCountsByDate.GetValueOrDefault(r.Date)))
            .OrderBy(p => p.Date)
            .ToList();
    }
}
