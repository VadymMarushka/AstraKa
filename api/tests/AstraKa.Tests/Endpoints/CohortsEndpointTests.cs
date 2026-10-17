using System.Net;
using System.Net.Http.Json;
using AstraKa.Api.Controllers;
using AstraKa.Application.Cohorts;
using AstraKa.Application.Common;
using AstraKa.Application.Courses;
using AstraKa.Domain.Enums;
using AstraKa.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Cohort ("Constellation") CRUD against the seeded accounts and courses. Seeded cohorts, all
/// dates relative to seed-time "today": HOMEWARD (author Mira) has homewardA (SeatLimit 10, start
/// today-40, Finished, 5 taken) and homewardB (SeatLimit 10, start today+14, NotStarted, 2 taken).
/// For Cancers (author Desmond) has one cohort, SeatLimit 6, start today-2, Running, 2 taken.
/// Saturn Return Recovery (author Desmond) has one cohort, SeatLimit 3, start today+7, NotStarted,
/// 2 taken (1 free seat). Throwaway cohorts in these tests only ever attach to HOMEWARD or a fresh
/// draft course, with distinctive SeatLimit values, so the seeded single-cohort courses (For
/// Cancers/Saturn) are never touched by another test and .Single() lookups against them stay safe.
/// </summary>
[Collection("Database")]
public class CohortsEndpointTests(DatabaseFixture fixture)
{
    private const string MiraSub = "auth0|mira-okonkwo";
    private const string DesmondSub = "auth0|desmond-vale";
    private const string TomaszSub = "auth0|tomasz-rienko";

    [Fact]
    public async Task Listing_upcoming_only_hides_finished_cohorts()
    {
        using var client = fixture.CreateClientAs(TomaszSub);
        var homewardId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();

        var response = await client.GetAsync($"/api/v1/courses/{homewardId}/cohorts?upcomingOnly=true&pageSize=50");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CohortDto>>(DatabaseFixture.JsonOptions);

        Assert.Contains(result!.Items, c => c.SeatLimit == 10 && c.Phase == CohortPhase.NotStarted);
        Assert.DoesNotContain(result.Items, c => c.Phase == CohortPhase.Finished);
    }

    [Fact]
    public async Task Listing_cohorts_of_a_draft_course_returns_404_for_a_non_author()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        using var desmondClient = fixture.CreateClientAs(DesmondSub);

        var draftId = await CreateDraftCourseAsync(miraClient);

        var response = await desmondClient.GetAsync($"/api/v1/courses/{draftId}/cohorts");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_as_course_author_returns_201()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var homewardId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        var response = await client.PostAsJsonAsync($"/api/v1/courses/{homewardId}/cohorts", new CreateCohortRequest(startDate, 7));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Create_as_another_Starshepherd_returns_403()
    {
        using var client = fixture.CreateClientAs(DesmondSub);
        var homewardId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        var response = await client.PostAsJsonAsync($"/api/v1/courses/{homewardId}/cohorts", new CreateCohortRequest(startDate, 7));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_a_StartDate_in_the_past_returns_409()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var homewardId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var response = await client.PostAsJsonAsync($"/api/v1/courses/{homewardId}/cohorts", new CreateCohortRequest(startDate, 7));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Editing_a_running_cohort_returns_409()
    {
        using var client = fixture.CreateClientAs(DesmondSub);
        var forCancersCohortId = await fixture.DbContext.Cohorts
            .Where(c => c.Course.Name == "For Cancers")
            .Select(c => c.Id)
            .SingleAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        var response = await client.PutAsJsonAsync($"/api/v1/cohorts/{forCancersCohortId}", new UpdateCohortRequest(startDate, 6));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Shrinking_SeatLimit_below_SeatsTaken_returns_409()
    {
        using var client = fixture.CreateClientAs(DesmondSub);
        var saturnReturnCohortId = await fixture.DbContext.Cohorts
            .Where(c => c.Course.Name == "Saturn Return Recovery")
            .Select(c => c.Id)
            .SingleAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);

        // Saturn Return Recovery's seeded cohort is NotStarted with 2 seats taken - shrinking to 1
        // isolates the seat-limit guard from the "already started" guard.
        var response = await client.PutAsJsonAsync($"/api/v1/cohorts/{saturnReturnCohortId}", new UpdateCohortRequest(startDate, 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_cohort_with_enrollments_returns_409()
    {
        using var client = fixture.CreateClientAs(DesmondSub);
        var saturnReturnCohortId = await fixture.DbContext.Cohorts
            .Where(c => c.Course.Name == "Saturn Return Recovery")
            .Select(c => c.Id)
            .SingleAsync();

        var response = await client.DeleteAsync($"/api/v1/cohorts/{saturnReturnCohortId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_fresh_cohort_returns_204()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var homewardId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();
        var id = await CreateCohortAsync(client, homewardId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), 77);

        var response = await client.DeleteAsync($"/api/v1/cohorts/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Moving_an_upcoming_cohorts_StartDate_into_the_past_returns_409()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var homewardId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();
        var id = await CreateCohortAsync(client, homewardId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), 78);

        var response = await client.PutAsJsonAsync($"/api/v1/cohorts/{id}", new UpdateCohortRequest(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), 78));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Updating_a_fresh_upcoming_cohort_returns_204_and_the_new_values_stick()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var homewardId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();
        var id = await CreateCohortAsync(client, homewardId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), 79);

        var newStartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(45);
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/cohorts/{id}", new UpdateCohortRequest(newStartDate, 12));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/cohorts/{id}");
        var details = await getResponse.Content.ReadFromJsonAsync<CohortDto>(DatabaseFixture.JsonOptions);

        Assert.Equal(newStartDate, details!.StartDate);
        Assert.Equal(12, details.SeatLimit);
    }

    private static async Task<Guid> CreateDraftCourseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/courses", new CreateCourseCommand(
            $"Draft Course - Cohort Probe {Guid.NewGuid()}", "A description.", "A promise.", 9.99m, []));
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location!.ToString();
        return Guid.Parse(location.Split('/').Last());
    }

    private static async Task<Guid> CreateCohortAsync(HttpClient client, Guid courseId, DateOnly startDate, int seatLimit)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/courses/{courseId}/cohorts", new CreateCohortRequest(startDate, seatLimit));
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location!.ToString();
        return Guid.Parse(location.Split('/').Last());
    }
}
