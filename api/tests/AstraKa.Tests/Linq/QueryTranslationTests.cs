using AstraKa.Domain.Enums;
using AstraKa.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace AstraKa.Tests.Linq;

/// <summary>
/// For the lab report: proves the EF Core LINQ shapes behind <c>GetExercisesQuery</c>,
/// <c>GetCoursesQuery</c> and <c>GetHarmonyTrendQuery</c> translate to a single SQL statement each,
/// rather than silently falling back to client-side evaluation.
/// </summary>
[Collection("Database")]
public class QueryTranslationTests(DatabaseFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public void Three_EF_LINQ_queries_translate_to_SQL()
    {
        // Same shape as GetExercisesQueryHandler's listing query.
        var exerciseListQuery = fixture.DbContext.Exercises
            .AsNoTracking()
            .Where(e => e.Unit == ExerciseUnit.Reps)
            .OrderBy(e => e.Name)
            .Select(e => new { e.Id, e.Name, e.Unit, e.AuthorId });

        // Same shape as GetCoursesQueryHandler's listing query.
        var courseListQuery = fixture.DbContext.Courses
            .AsNoTracking()
            .Where(c => c.Status == CourseStatus.Published)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Price, DayCount = c.CourseDays.Count });

        // Same shape as GetHarmonyTrendQueryHandler's two GroupBy queries.
        var harmonyTrendQuery = fixture.DbContext.DailyReports
            .AsNoTracking()
            .GroupBy(r => r.Date)
            .Select(g => new { Date = g.Key, Average = g.Average(r => (double)r.HarmonyLevel), Count = g.Count() });

        var completionCountsQuery = fixture.DbContext.ExerciseCompletions
            .AsNoTracking()
            .GroupBy(c => c.DailyReport.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() });

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
