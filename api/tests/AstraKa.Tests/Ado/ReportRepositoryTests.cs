using AstraKa.Application.Reports;
using AstraKa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Tests.Ado;

[Collection("Database")]
public class ReportRepositoryTests(DatabaseFixture fixture)
{
    private async Task<Guid> GetHomewardFinishedCohortIdAsync()
    {
        var homewardCourseId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();

        // Cohort A (finished) started well in the past; Cohort B (upcoming) starts in the future -
        // the earliest StartDate is always Cohort A, regardless of exactly when the seeder ran.
        return await fixture.DbContext.Cohorts
            .Where(c => c.CourseId == homewardCourseId)
            .OrderBy(c => c.StartDate)
            .Select(c => c.Id)
            .FirstAsync();
    }

    [Fact]
    public async Task GetCohortParticipantsAsync_returns_every_participant_including_the_one_who_left()
    {
        var cohortId = await GetHomewardFinishedCohortIdAsync();

        var participants = await fixture.Reports.GetCohortParticipantsAsync(cohortId, null, CohortParticipantSort.Name, false, CancellationToken.None);

        Assert.Equal(5, participants.Count);
        Assert.Contains(participants, p => p.WandererName == "Yuki Tanaka" && p.Status == "Left");
    }

    [Fact]
    public async Task GetCohortParticipantsAsync_status_filter_excludes_the_one_who_left()
    {
        var cohortId = await GetHomewardFinishedCohortIdAsync();

        var active = await fixture.Reports.GetCohortParticipantsAsync(cohortId, EnrollmentStatus.Active, CohortParticipantSort.Name, false, CancellationToken.None);

        Assert.Equal(4, active.Count);
        Assert.DoesNotContain(active, p => p.WandererName == "Yuki Tanaka");
    }

    [Fact]
    public async Task GetCohortParticipantsAsync_sorts_by_name_ascending()
    {
        var cohortId = await GetHomewardFinishedCohortIdAsync();

        var participants = await fixture.Reports.GetCohortParticipantsAsync(cohortId, null, CohortParticipantSort.Name, false, CancellationToken.None);

        var sortedCopy = participants.OrderBy(p => p.WandererName, StringComparer.Ordinal).ToList();
        Assert.Equal(sortedCopy.Select(p => p.WandererName), participants.Select(p => p.WandererName));
    }

    [Fact]
    public async Task GetCourseStatisticsAsync_reports_HOMEWARDs_enrollment_count_revenue_and_average_harmony()
    {
        var statistics = await fixture.Reports.GetCourseStatisticsAsync(CancellationToken.None);

        var homeward = Assert.Single(statistics, s => s.CourseName == "HOMEWARD");
        // HOMEWARD has 5 enrollments in the finished cohort and 2 in the upcoming one.
        Assert.Equal(7, homeward.EnrollmentCount);
        Assert.Equal(7 * 19.99m, homeward.TotalRevenue);
        Assert.NotNull(homeward.AverageHarmonyLevel);
        Assert.InRange(homeward.AverageHarmonyLevel!.Value, 1, 10);
    }
}
