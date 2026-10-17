using System.Net;
using System.Net.Http.Json;
using AstraKa.Api.Controllers;
using AstraKa.Application.Common;
using AstraKa.Application.Courses;
using AstraKa.Application.Enrollments;
using AstraKa.Application.Me;
using AstraKa.Domain.Entities;
using AstraKa.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Enrollment ("joining a Constellation") against entirely throwaway data - a fresh course/cohort
/// per scenario (authored by seeded Starshepherd Mira) and fresh Wanderers onboarded with chosen
/// zodiac signs via brand-new, never-seeded subs, so nothing here touches seeded cohorts/enrollments
/// (the one deliberate exception, test 10, reuses seeded data safely - see its own comment).
/// </summary>
[Collection("Database")]
public class EnrollmentsEndpointTests(DatabaseFixture fixture)
{
    private const string MiraSub = "auth0|mira-okonkwo";
    private const string TomaszSub = "auth0|tomasz-rienko";

    [Fact]
    public async Task Happy_path_enrolls_increments_seats_and_shows_up_as_Active()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        var cohortId = await CreatePublishedCohortAsync(miraClient, signId: 2, price: 49.99m);

        using var wandererClient = await OnboardFreshWandererAsync(signId: 2);

        var response = await wandererClient.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var seatsTaken = await fixture.DbContext.Cohorts.Where(c => c.Id == cohortId).Select(c => c.SeatsTaken).SingleAsync();
        Assert.Equal(1, seatsTaken);

        var listResponse = await wandererClient.GetAsync("/api/v1/me/enrollments?pageSize=50");
        var result = await listResponse.Content.ReadFromJsonAsync<PagedResult<MyEnrollmentDto>>(DatabaseFixture.JsonOptions);
        var mine = result!.Items.Single(e => e.CohortId == cohortId);
        Assert.Equal(EnrollmentDisplayStatus.Active, mine.Status);
        Assert.Equal(49.99m, mine.PaidPrice);
    }

    [Fact]
    public async Task Enrolling_in_a_draft_course_returns_409()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        var courseId = await CreateCourseAsync(miraClient, 3); // never published
        var cohortId = await CreateCohortAsync(miraClient, courseId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), 5);

        using var wandererClient = await OnboardFreshWandererAsync(signId: 3);

        var response = await wandererClient.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrolling_in_an_already_started_cohort_returns_409()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        var courseId = await CreateCourseAsync(miraClient, 4);
        await PublishAsync(miraClient, courseId);

        // The Cohorts API itself refuses a past StartDate on create - this is the only way to get
        // a throwaway already-started cohort for this scenario.
        var cohort = new Cohort(Guid.NewGuid(), courseId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), 5);
        fixture.DbContext.Cohorts.Add(cohort);
        await fixture.DbContext.SaveChangesAsync();

        using var wandererClient = await OnboardFreshWandererAsync(signId: 4);

        var response = await wandererClient.PostAsync($"/api/v1/cohorts/{cohort.Id}/enrollments", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrolling_with_no_free_seats_returns_409()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        var cohortId = await CreatePublishedCohortAsync(miraClient, signId: 5, seatLimit: 1);

        using var firstWanderer = await OnboardFreshWandererAsync(signId: 5);
        var firstResponse = await firstWanderer.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var secondWanderer = await OnboardFreshWandererAsync(signId: 5);
        var secondResponse = await secondWanderer.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Enrolling_with_a_disallowed_sign_returns_409()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        var cohortId = await CreatePublishedCohortAsync(miraClient, signId: 6); // course open only to sign 6

        using var wandererClient = await OnboardFreshWandererAsync(signId: 7); // different sign

        var response = await wandererClient.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrolling_twice_returns_409_the_second_time()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        var cohortId = await CreatePublishedCohortAsync(miraClient, signId: 8);

        using var wandererClient = await OnboardFreshWandererAsync(signId: 8);

        var firstResponse = await wandererClient.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await wandererClient.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task A_Starshepherd_cannot_enroll()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        var cohortId = await CreatePublishedCohortAsync(miraClient, signId: 9);

        var response = await miraClient.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Leaving_succeeds_once_and_409s_the_second_time()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        var cohortId = await CreatePublishedCohortAsync(miraClient, signId: 10);

        using var wandererClient = await OnboardFreshWandererAsync(signId: 10);
        await wandererClient.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null);

        var seatsAfterEnroll = await fixture.DbContext.Cohorts.Where(c => c.Id == cohortId).Select(c => c.SeatsTaken).SingleAsync();

        var leaveResponse = await wandererClient.PostAsync($"/api/v1/me/enrollments/{cohortId}/leave", null);
        Assert.Equal(HttpStatusCode.NoContent, leaveResponse.StatusCode);

        var detailsResponse = await wandererClient.GetAsync($"/api/v1/me/enrollments/{cohortId}");
        var details = await detailsResponse.Content.ReadFromJsonAsync<MyEnrollmentDto>(DatabaseFixture.JsonOptions);
        Assert.Equal(EnrollmentDisplayStatus.Left, details!.Status);

        var seatsAfterLeave = await fixture.DbContext.Cohorts.Where(c => c.Id == cohortId).Select(c => c.SeatsTaken).SingleAsync();
        Assert.Equal(seatsAfterEnroll, seatsAfterLeave); // the seat is never freed

        var secondLeaveResponse = await wandererClient.PostAsync($"/api/v1/me/enrollments/{cohortId}/leave", null);
        Assert.Equal(HttpStatusCode.Conflict, secondLeaveResponse.StatusCode);
    }

    [Fact]
    public async Task Racing_for_the_last_seat_lets_exactly_one_enrollment_through()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        var cohortId = await CreatePublishedCohortAsync(miraClient, signId: 11, seatLimit: 1);

        using var firstWanderer = await OnboardFreshWandererAsync(signId: 11);
        using var secondWanderer = await OnboardFreshWandererAsync(signId: 11);

        var responses = await Task.WhenAll(
            firstWanderer.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null),
            secondWanderer.PostAsync($"/api/v1/cohorts/{cohortId}/enrollments", null));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var seatsTaken = await fixture.DbContext.Cohorts.Where(c => c.Id == cohortId).Select(c => c.SeatsTaken).SingleAsync();
        Assert.Equal(1, seatsTaken);
    }

    [Fact]
    public async Task Leaving_an_already_completed_enrollment_returns_409()
    {
        // Reuses seeded data deliberately: HOMEWARD's homewardA cohort is Finished, and Tomasz has
        // an Active enrollment in it. Safe because a rejected Leave() throws before any mutation -
        // nothing is persisted, so no other test is affected.
        using var tomaszClient = fixture.CreateClientAs(TomaszSub);
        var homewardACohortId = await fixture.DbContext.Cohorts
            .Where(c => c.Course.Name == "HOMEWARD" && c.SeatLimit == 10 && c.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
            .Select(c => c.Id)
            .SingleAsync();

        var response = await tomaszClient.PostAsync($"/api/v1/me/enrollments/{homewardACohortId}/leave", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<HttpClient> OnboardFreshWandererAsync(int signId)
    {
        var sub = $"auth0|test-wanderer-{Guid.NewGuid()}";
        var email = $"{Guid.NewGuid()}@astraka.test";
        var client = fixture.CreateClientAs(sub, email);

        var response = await client.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("Test Wanderer", signId));
        response.EnsureSuccessStatusCode();

        return client;
    }

    private async Task<Guid> CreateCourseAsync(HttpClient miraClient, int zodiacSignId)
    {
        var response = await miraClient.PostAsJsonAsync("/api/v1/courses", new CreateCourseCommand(
            $"Throwaway Course {Guid.NewGuid()}", "A description.", "A promise.", 49.99m, [zodiacSignId]));
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location!.ToString();
        var courseId = Guid.Parse(location.Split('/').Last());

        await fixture.CourseDays.CreateAsync(courseId, 1, "Day One", null, null, CancellationToken.None);

        return courseId;
    }

    private static async Task PublishAsync(HttpClient miraClient, Guid courseId)
    {
        var response = await miraClient.PostAsync($"/api/v1/courses/{courseId}/publish", null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> CreateCohortAsync(HttpClient miraClient, Guid courseId, DateOnly startDate, int seatLimit)
    {
        var response = await miraClient.PostAsJsonAsync($"/api/v1/courses/{courseId}/cohorts", new CreateCohortRequest(startDate, seatLimit));
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location!.ToString();
        return Guid.Parse(location.Split('/').Last());
    }

    private async Task<Guid> CreatePublishedCohortAsync(HttpClient miraClient, int signId, int seatLimit = 10, decimal price = 49.99m)
    {
        var response = await miraClient.PostAsJsonAsync("/api/v1/courses", new CreateCourseCommand(
            $"Throwaway Course {Guid.NewGuid()}", "A description.", "A promise.", price, [signId]));
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location!.ToString();
        var courseId = Guid.Parse(location.Split('/').Last());

        await fixture.CourseDays.CreateAsync(courseId, 1, "Day One", null, null, CancellationToken.None);
        await PublishAsync(miraClient, courseId);

        return await CreateCohortAsync(miraClient, courseId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), seatLimit);
    }
}
