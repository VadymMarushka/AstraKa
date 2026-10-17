using System.Net;
using System.Net.Http.Json;
using AstraKa.Api.Controllers;
using AstraKa.Application.CourseProgram;
using AstraKa.Application.Courses;
using AstraKa.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Course program (days + day exercises) CRUD against entirely throwaway courses authored by
/// seeded Starshepherd Mira - never touches seeded program data.
/// </summary>
[Collection("Database")]
public class CourseProgramEndpointTests(DatabaseFixture fixture)
{
    private const string MiraSub = "auth0|mira-okonkwo";

    [Fact]
    public async Task Day_CRUD_happy_path()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var courseId = await CreateThrowawayCourseAsync(client);

        var createResponse = await client.PostAsJsonAsync($"/api/v1/courses/{courseId}/days", new CreateDayRequest(1, "Day One", "A description.", null));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var dayId = Guid.Parse(createResponse.Headers.Location!.ToString().Split('/').Last());

        var daysAfterCreate = await GetDaysAsync(client, courseId);
        Assert.Contains(daysAfterCreate, d => d.Id == dayId && d.Title == "Day One");

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/days/{dayId}", new UpdateDayRequest("Day One Updated", "New description.", "https://meet.astraka.app/x"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var daysAfterUpdate = await GetDaysAsync(client, courseId);
        Assert.Contains(daysAfterUpdate, d => d.Id == dayId && d.Title == "Day One Updated");

        var deleteResponse = await client.DeleteAsync($"/api/v1/days/{dayId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var daysAfterDelete = await GetDaysAsync(client, courseId);
        Assert.DoesNotContain(daysAfterDelete, d => d.Id == dayId);
    }

    [Fact]
    public async Task Duplicate_DayNumber_returns_409()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var courseId = await CreateThrowawayCourseAsync(client);

        var firstResponse = await client.PostAsJsonAsync($"/api/v1/courses/{courseId}/days", new CreateDayRequest(1, "Day One", null, null));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync($"/api/v1/courses/{courseId}/days", new CreateDayRequest(1, "Day One Again", null, null));
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Day_exercise_CRUD_and_non_positive_Sets_returns_400()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var courseId = await CreateThrowawayCourseAsync(client);
        var dayId = await fixture.CourseDays.CreateAsync(courseId, 1, "Day One", null, null, CancellationToken.None);
        var beacon = await fixture.GetExerciseIdAsync("Beacon");

        var createResponse = await client.PostAsJsonAsync($"/api/v1/days/{dayId}/exercises", new CreateDayExerciseRequest(beacon, 1, 2, 20));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var dayExerciseId = Guid.Parse(createResponse.Headers.Location!.ToString().Split('/').Last());

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/day-exercises/{dayExerciseId}", new UpdateDayExerciseRequest(2, 3, 25));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/v1/day-exercises/{dayExerciseId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var invalidResponse = await client.PostAsJsonAsync($"/api/v1/days/{dayId}/exercises", new CreateDayExerciseRequest(beacon, 1, 0, 20));
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task With_program_commits_the_course_and_its_full_program()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var resonance = await fixture.GetExerciseIdAsync("Resonance");
        var orbit = await fixture.GetExerciseIdAsync("Orbit");
        var courseName = $"With-Program Commit Probe {Guid.NewGuid()}";

        var command = new CreateCourseWithProgramCommand(
            courseName, "A description.", "A promise.", 11.11m,
            new[]
            {
                new CreateCourseWithProgramDayInput(1, "Day One", null, null, new[] { new CreateCourseWithProgramExerciseInput(resonance, 1, 2, 6) }),
                new CreateCourseWithProgramDayInput(2, "Day Two", "Second day.", null, new[]
                {
                    new CreateCourseWithProgramExerciseInput(orbit, 1, 2, 30),
                    new CreateCourseWithProgramExerciseInput(resonance, 2, 2, 6),
                }),
            });

        var response = await client.PostAsJsonAsync("/api/v1/courses/with-program", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var courseId = await fixture.DbContext.Courses.Where(c => c.Name == courseName).Select(c => c.Id).SingleAsync();
        var dayCount = await fixture.DbContext.CourseDays.CountAsync(d => d.CourseId == courseId);
        var dayExerciseCount = await fixture.DbContext.DayExercises.CountAsync(de => de.CourseDay.CourseId == courseId);

        Assert.Equal(2, dayCount);
        Assert.Equal(3, dayExerciseCount);
    }

    [Fact]
    public async Task With_program_rolls_back_everything_on_a_non_existent_exercise()
    {
        using var client = fixture.CreateClientAs(MiraSub);
        var realExercise = await fixture.GetExerciseIdAsync("Beacon");
        var missingExercise = Guid.NewGuid();
        var courseName = $"With-Program Rollback Probe {Guid.NewGuid()}";

        var command = new CreateCourseWithProgramCommand(
            courseName, "A description.", "A promise.", 22.22m,
            new[]
            {
                new CreateCourseWithProgramDayInput(1, "Day One", null, null, new[] { new CreateCourseWithProgramExerciseInput(realExercise, 1, 2, 20) }),
                new CreateCourseWithProgramDayInput(2, "Day Two", null, null, new[] { new CreateCourseWithProgramExerciseInput(missingExercise, 1, 2, 20) }),
            });

        var response = await client.PostAsJsonAsync("/api/v1/courses/with-program", command);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var courseExists = await fixture.DbContext.Courses.AnyAsync(c => c.Name == courseName);
        Assert.False(courseExists);
    }

    private static async Task<Guid> CreateThrowawayCourseAsync(HttpClient miraClient)
    {
        var response = await miraClient.PostAsJsonAsync("/api/v1/courses", new CreateCourseCommand(
            $"Throwaway Course {Guid.NewGuid()}", "A description.", "A promise.", 9.99m, []));
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location!.ToString();
        return Guid.Parse(location.Split('/').Last());
    }

    private static async Task<IReadOnlyList<CourseDayWithExercisesRecord>> GetDaysAsync(HttpClient client, Guid courseId)
    {
        var response = await client.GetAsync($"/api/v1/courses/{courseId}/days");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<CourseDayWithExercisesRecord>>(DatabaseFixture.JsonOptions))!;
    }
}
