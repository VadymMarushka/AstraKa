using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
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
        var reportStats = await BuildReportStatsQuery(db.DailyReports.AsNoTracking(), request.CohortId).ToListAsync(cancellationToken);

        var completionCountsByDate = await BuildCompletionCountsQuery(db.ExerciseCompletions.AsNoTracking(), request.CohortId)
            .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

        return reportStats
            .Select(r => new HarmonyTrendPointDto(r.Date, r.Average, r.Count, completionCountsByDate.GetValueOrDefault(r.Date)))
            .OrderBy(p => p.Date)
            .ToList();
    }

    /// <summary>
    /// The report-stats half of the trend (average harmony level + report count per date), scoped
    /// to one cohort - extracted so <c>QueryTranslationTests</c> can capture this handler's real SQL
    /// shape via <c>IQueryable.ToQueryString()</c> instead of re-implementing a simplified stand-in.
    /// </summary>
    internal static IQueryable<ReportStatsRow> BuildReportStatsQuery(IQueryable<DailyReport> dailyReports, Guid cohortId) =>
        dailyReports
            .Where(r => r.CohortId == cohortId)
            .GroupBy(r => r.Date)
            .Select(g => new ReportStatsRow(g.Key, g.Average(r => (double)r.HarmonyLevel), g.Count()));

    /// <summary>The completed-exercise-count half of the trend, scoped to the same cohort.</summary>
    internal static IQueryable<CompletionCountRow> BuildCompletionCountsQuery(IQueryable<ExerciseCompletion> completions, Guid cohortId) =>
        completions
            .Where(c => c.DailyReport.CohortId == cohortId)
            .GroupBy(c => c.DailyReport.Date)
            .Select(g => new CompletionCountRow(g.Key, g.Count()));

    internal sealed record ReportStatsRow(DateOnly Date, double Average, int Count);

    internal sealed record CompletionCountRow(DateOnly Date, int Count);
}
