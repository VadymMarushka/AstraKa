using System.Net;
using System.Net.Http.Json;
using AstraKa.Api.Controllers;
using AstraKa.Application.Common;
using AstraKa.Application.Exercises;
using AstraKa.Domain.Enums;
using AstraKa.Tests.Common;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Exercise catalog CRUD against the seeded accounts: Mira/Desmond (Starshepherds), Tomasz
/// (Wanderer), and the seeded Oracle - no new accounts need onboarding for most of these.
/// </summary>
[Collection("Database")]
public class ExercisesEndpointTests(DatabaseFixture fixture)
{
    private const string MiraSub = "auth0|mira-okonkwo";
    private const string DesmondSub = "auth0|desmond-vale";
    private const string TomaszSub = "auth0|tomasz-rienko";
    private const string OracleSub = "auth0|oracle";

    [Fact]
    public async Task List_with_pagination_and_a_filter_returns_only_the_matching_seeded_exercises()
    {
        using var client = fixture.CreateClientAs(TomaszSub);

        var response = await client.GetAsync("/api/v1/exercises?unit=Seconds&systemOnly=true&page=1&pageSize=5");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ExerciseDto>>(DatabaseFixture.JsonOptions);

        response.EnsureSuccessStatusCode();
        Assert.Equal(1, result!.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(5, result.TotalCount); // Beacon, Span, Meridian, Orbit, Lone Watcher
        Assert.Equal(1, result.TotalPages);
        Assert.True(result.Items.Count <= 5);
        Assert.All(result.Items, item => Assert.True(item.IsSystemExercise));
    }

    [Fact]
    public async Task Create_as_starshepherd_returns_201_with_a_location_header()
    {
        using var client = fixture.CreateClientAs(MiraSub);

        var response = await client.PostAsJsonAsync("/api/v1/exercises", new CreateExerciseCommand(
            "Test Create Exercise", "A lore line.", "How to do it.", ExerciseUnit.Seconds, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Create_as_wanderer_returns_403()
    {
        using var client = fixture.CreateClientAs(TomaszSub);

        var response = await client.PostAsJsonAsync("/api/v1/exercises", new CreateExerciseCommand(
            "Should Not Be Created", "A lore line.", "How to do it.", ExerciseUnit.Reps, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_by_a_non_author_starshepherd_returns_403()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        using var desmondClient = fixture.CreateClientAs(DesmondSub);

        var createResponse = await miraClient.PostAsJsonAsync("/api/v1/exercises", new CreateExerciseCommand(
            "Mira's Exercise", "A lore line.", "How to do it.", ExerciseUnit.Breaths, null));
        var id = ExtractIdFromLocation(createResponse);

        var updateResponse = await desmondClient.PutAsJsonAsync($"/api/v1/exercises/{id}", new UpdateExerciseRequest(
            "Hijacked", "New lore.", "New instructions.", ExerciseUnit.Breaths, null));

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Update_by_the_author_returns_204()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);

        var createResponse = await miraClient.PostAsJsonAsync("/api/v1/exercises", new CreateExerciseCommand(
            "Mira's Other Exercise", "A lore line.", "How to do it.", ExerciseUnit.Reps, null));
        var id = ExtractIdFromLocation(createResponse);

        var updateResponse = await miraClient.PutAsJsonAsync($"/api/v1/exercises/{id}", new UpdateExerciseRequest(
            "Mira's Updated Exercise", "Updated lore.", "Updated instructions.", ExerciseUnit.Reps, null));

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Update_a_system_exercise_by_oracle_returns_204()
    {
        using var oracleClient = fixture.CreateClientAs(OracleSub);

        var beaconId = await FindExerciseIdByNameAsync(oracleClient, "Beacon");

        // Name is deliberately left unchanged - other tests/the ADO.NET suite look "Beacon" up by
        // name in the same shared AstraKa_Tests database.
        var updateResponse = await oracleClient.PutAsJsonAsync($"/api/v1/exercises/{beaconId}", new UpdateExerciseRequest(
            "Beacon", "A slightly different lore line, set by this test.", "Lie on your side with your legs stacked.", ExerciseUnit.Seconds, null));

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_an_exercise_used_in_a_course_program_returns_409()
    {
        using var oracleClient = fixture.CreateClientAs(OracleSub);

        var beaconId = await FindExerciseIdByNameAsync(oracleClient, "Beacon"); // seeded into HOMEWARD day 5

        var deleteResponse = await oracleClient.DeleteAsync($"/api/v1/exercises/{beaconId}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Update_and_delete_a_system_exercise_by_a_non_oracle_starshepherd_both_return_403()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);

        var beaconId = await FindExerciseIdByNameAsync(miraClient, "Beacon"); // AuthorId is null - only Oracle may match it

        var updateResponse = await miraClient.PutAsJsonAsync($"/api/v1/exercises/{beaconId}", new UpdateExerciseRequest(
            "Beacon", "Should not be applied.", "Should not be applied.", ExerciseUnit.Seconds, null));
        var deleteResponse = await miraClient.DeleteAsync($"/api/v1/exercises/{beaconId}");

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_an_unused_exercise_returns_204()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);

        var createResponse = await miraClient.PostAsJsonAsync("/api/v1/exercises", new CreateExerciseCommand(
            "Throwaway Delete Target", "A lore line.", "How to do it.", ExerciseUnit.Seconds, null));
        var id = ExtractIdFromLocation(createResponse);

        var deleteResponse = await miraClient.DeleteAsync($"/api/v1/exercises/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Get_by_a_missing_id_returns_404()
    {
        using var client = fixture.CreateClientAs(TomaszSub);

        var response = await client.GetAsync($"/api/v1/exercises/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Guid ExtractIdFromLocation(HttpResponseMessage response)
    {
        var location = response.Headers.Location!.ToString();
        return Guid.Parse(location.Split('/').Last());
    }

    private static async Task<Guid> FindExerciseIdByNameAsync(HttpClient client, string name)
    {
        var response = await client.GetAsync($"/api/v1/exercises?nameSearch={Uri.EscapeDataString(name)}&systemOnly=true");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ExerciseDto>>(DatabaseFixture.JsonOptions);
        return result!.Items.Single(e => e.Name == name).Id;
    }
}
