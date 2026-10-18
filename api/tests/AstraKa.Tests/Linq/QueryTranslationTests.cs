using AstraKa.Application.Courses;
using AstraKa.Application.Exercises;
using AstraKa.Application.Reports;
using AstraKa.Domain.Enums;
using AstraKa.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace AstraKa.Tests.Linq;

/// <summary>
/// For the lab report: proves the EF Core LINQ shapes behind <c>GetExercisesQuery</c>,
/// <c>GetCoursesQuery</c> and <c>GetHarmonyTrendQuery</c> translate to a single SQL statement each,
/// rather than silently falling back to client-side evaluation. Captures the handlers' *real*
/// queries - built by the same <c>internal static</c> methods the handlers themselves call
/// (<c>GetExercisesQueryHandler.BuildFilteredQuery</c>/<c>BuildPagedQuery</c> and so on) - with
/// realistic filter/sort/pagination values, not a simplified re-implementation.
/// </summary>
[Collection("Database")]
public class QueryTranslationTests(DatabaseFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public void Three_EF_LINQ_queries_translate_to_SQL()
    {
        // The exercise list's items query, as GetExercisesQueryHandler actually builds it: name
        // search + unit filter + sort + pagination, all the way down to the final Select.
        var exercisesRequest = new GetExercisesQuery(Unit: ExerciseUnit.Reps, NameSearch: "Stretch", Page: 2, PageSize: 10);
        var exerciseListQuery = GetExercisesQueryHandler.BuildPagedQuery(
            GetExercisesQueryHandler.BuildFilteredQuery(fixture.DbContext.Exercises.AsNoTracking(), exercisesRequest),
            exercisesRequest);

        // The course list's items query, as GetCoursesQueryHandler actually builds it: a non-Oracle,
        // non-mine caller (so the Published-only filter kicks in) plus sign/name/price filters, the
        // author/sign joins in the projection, sort, and pagination.
        var coursesRequest = new GetCoursesQuery(NameSearch: "Homeward", SignId: 1, MinPrice: 5m, MaxPrice: 50m, Page: 1, PageSize: 10);
        var courseListQuery = GetCoursesQueryHandler.BuildPagedQuery(
            GetCoursesQueryHandler.BuildFilteredQuery(fixture.DbContext.Courses.AsNoTracking(), coursesRequest, currentUserId: null, currentUserIsOracle: false),
            coursesRequest);

        // The harmony trend's two GroupBy queries, as GetHarmonyTrendQueryHandler actually builds
        // them - both scoped to one cohort.
        var cohortId = Guid.NewGuid();
        var harmonyTrendQuery = GetHarmonyTrendQueryHandler.BuildReportStatsQuery(fixture.DbContext.DailyReports.AsNoTracking(), cohortId);
        var completionCountsQuery = GetHarmonyTrendQueryHandler.BuildCompletionCountsQuery(fixture.DbContext.ExerciseCompletions.AsNoTracking(), cohortId);

        var exerciseListSql = exerciseListQuery.ToQueryString();
        var courseListSql = courseListQuery.ToQueryString();
        var harmonyTrendSql = harmonyTrendQuery.ToQueryString();
        var completionCountsSql = completionCountsQuery.ToQueryString();

        output.WriteLine("Exercise list:" + Environment.NewLine + exerciseListSql);
        output.WriteLine(string.Empty);
        output.WriteLine("Course list:" + Environment.NewLine + courseListSql);
        output.WriteLine(string.Empty);
        output.WriteLine("Harmony trend (report stats):" + Environment.NewLine + harmonyTrendSql);
        output.WriteLine(string.Empty);
        output.WriteLine("Harmony trend (completion counts):" + Environment.NewLine + completionCountsSql);

        Assert.Contains("SELECT", exerciseListSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SELECT", courseListSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SELECT", harmonyTrendSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GROUP BY", harmonyTrendSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GROUP BY", completionCountsSql, StringComparison.OrdinalIgnoreCase);
    }
}
