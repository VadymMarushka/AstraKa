using System.Net;
using System.Net.Http.Json;
using AstraKa.Application.Reports;
using AstraKa.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Reports against seeded data only - reads never mutate anything, so these are safe to run
/// alongside everything else that depends on the seeded courses/cohorts existing as seeded.
/// </summary>
[Collection("Database")]
public class ReportsEndpointTests(DatabaseFixture fixture)
{
    private const string MiraSub = "auth0|mira-okonkwo";
    private const string PriyaSub = "auth0|priya-natarajan";

    [Fact]
    public async Task Participants_for_a_seeded_cohort_includes_the_one_who_left()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var homewardFinishedCohortId = await fixture.DbContext.Cohorts
            .Where(c => c.Course.Name == "HOMEWARD")
            .OrderBy(c => c.StartDate)
            .Select(c => c.Id)
            .FirstAsync();

        var response = await client.GetAsync($"/api/v1/cohorts/{homewardFinishedCohortId}/participants");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var participants = await response.Content.ReadFromJsonAsync<List<CohortParticipantRecord>>(DatabaseFixture.JsonOptions);
        Assert.Equal(5, participants!.Count);
        Assert.Contains(participants, p => p.WandererName == "Yuki Tanaka" && p.Status == "Left");
    }

    [Fact]
    public async Task Statistics_for_a_seeded_course_reports_enrollment_count_revenue_and_average_harmony()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var homewardId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();

        var response = await client.GetAsync($"/api/v1/courses/{homewardId}/statistics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var statistics = await response.Content.ReadFromJsonAsync<CourseStatisticsRecord>(DatabaseFixture.JsonOptions);
        Assert.Equal(7, statistics!.EnrollmentCount);
        Assert.Equal(7 * 19.99m, statistics.TotalRevenue);
        Assert.NotNull(statistics.AverageHarmonyLevel);
        Assert.InRange(statistics.AverageHarmonyLevel!.Value, 1, 10);
    }

    [Fact]
    public async Task Harmony_trend_for_a_seeded_running_cohort_averages_each_date_separately()
    {
        using var client = fixture.CreateClientAs(PriyaSub);
        var mercuryCohortId = await fixture.DbContext.Cohorts
            .Where(c => c.Course.Name == "Mercury Retrograde Survival")
            .Select(c => c.Id)
            .SingleAsync();

        var response = await client.GetAsync($"/api/v1/cohorts/{mercuryCohortId}/harmony-trend");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trend = await response.Content.ReadFromJsonAsync<List<HarmonyTrendPointDto>>(DatabaseFixture.JsonOptions);
        Assert.Equal(2, trend!.Count);

        var day1 = trend[0];
        Assert.Equal(4, day1.ReportCount);
        Assert.Equal(5.25, day1.AverageHarmonyLevel, 2);
        Assert.Equal(7, day1.CompletedExerciseCount);

        var day2 = trend[1];
        Assert.Equal(4, day2.ReportCount);
        Assert.Equal(5.75, day2.AverageHarmonyLevel, 2);
        Assert.Equal(7, day2.CompletedExerciseCount);
    }

    [Fact]
    public async Task Non_author_Starshepherd_gets_403_on_a_report()
    {
        using var client = fixture.CreateClientAs(MiraSub); // Mira does not author Mercury Retrograde Survival
        var mercuryCohortId = await fixture.DbContext.Cohorts
            .Where(c => c.Course.Name == "Mercury Retrograde Survival")
            .Select(c => c.Id)
            .SingleAsync();

        var response = await client.GetAsync($"/api/v1/cohorts/{mercuryCohortId}/participants");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
