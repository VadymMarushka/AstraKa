using System.Net;
using System.Net.Http.Json;
using AstraKa.Api.Controllers;
using AstraKa.Application.Common;
using AstraKa.Application.Courses;
using AstraKa.Tests.Common;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Course CRUD against the seeded accounts and the 4 already-published seeded courses
/// (HOMEWARD/Any, For Cancers/Cancer, Mercury Retrograde Survival/Any, Saturn Return
/// Recovery/Capricorn+Aquarius+Pisces). Every list/filter assertion checks by seeded course name
/// (Contains/DoesNotContain), never an exact count - other tests here create throwaway courses a
/// broad listing would also pick up. Throwaway names are distinctive and their prices (99.99)
/// sit outside the 14-15 window the price filter test uses.
/// </summary>
[Collection("Database")]
public class CoursesEndpointTests(DatabaseFixture fixture)
{
    private const string MiraSub = "auth0|mira-okonkwo";
    private const string DesmondSub = "auth0|desmond-vale";
    private const string TomaszSub = "auth0|tomasz-rienko";
    private const string OracleSub = "auth0|oracle";

    [Fact]
    public async Task Wanderer_does_not_see_a_draft()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        using var tomaszClient = fixture.CreateClientAs(TomaszSub);

        var draftName = $"Mira's Draft - Visibility Probe {Guid.NewGuid()}";
        await CreateCourseAsync(miraClient, draftName, 99.99m, []);

        var response = await tomaszClient.GetAsync($"/api/v1/courses?nameSearch={Uri.EscapeDataString(draftName)}");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CourseListItemDto>>(DatabaseFixture.JsonOptions);

        Assert.DoesNotContain(result!.Items, c => c.Name == draftName);
    }

    [Fact]
    public async Task Sign_filter_matches_For_Cancers_only_for_Cancer()
    {
        using var client = fixture.CreateClientAs(TomaszSub);

        var cancerResponse = await client.GetAsync("/api/v1/courses?signId=4&pageSize=50"); // Cancer
        var cancerResult = await cancerResponse.Content.ReadFromJsonAsync<PagedResult<CourseListItemDto>>(DatabaseFixture.JsonOptions);
        Assert.Contains(cancerResult!.Items, c => c.Name == "For Cancers");
        Assert.Contains(cancerResult.Items, c => c.Name == "HOMEWARD");

        var ariesResponse = await client.GetAsync("/api/v1/courses?signId=1&pageSize=50"); // Aries
        var ariesResult = await ariesResponse.Content.ReadFromJsonAsync<PagedResult<CourseListItemDto>>(DatabaseFixture.JsonOptions);
        Assert.DoesNotContain(ariesResult!.Items, c => c.Name == "For Cancers");
        Assert.Contains(ariesResult.Items, c => c.Name == "HOMEWARD");
        Assert.Contains(ariesResult.Items, c => c.Name == "Mercury Retrograde Survival");
    }

    [Fact]
    public async Task Price_range_filter_matches_only_Mercury_Retrograde_Survival()
    {
        using var client = fixture.CreateClientAs(TomaszSub);

        var response = await client.GetAsync("/api/v1/courses?minPrice=14&maxPrice=15&pageSize=50");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CourseListItemDto>>(DatabaseFixture.JsonOptions);

        Assert.Contains(result!.Items, c => c.Name == "Mercury Retrograde Survival"); // 14.99
        Assert.DoesNotContain(result.Items, c => c.Name == "HOMEWARD"); // 19.99
        Assert.DoesNotContain(result.Items, c => c.Name == "For Cancers"); // 24.99
        Assert.DoesNotContain(result.Items, c => c.Name == "Saturn Return Recovery"); // 29.99
    }

    [Fact]
    public async Task Mine_shows_the_authors_own_draft()
    {
        using var client = fixture.CreateClientAs(MiraSub);

        var draftName = $"Mira's Draft - Mine Probe {Guid.NewGuid()}";
        await CreateCourseAsync(client, draftName, 99.99m, []);

        var response = await client.GetAsync("/api/v1/courses?mine=true&pageSize=50");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CourseListItemDto>>(DatabaseFixture.JsonOptions);

        Assert.Contains(result!.Items, c => c.Name == draftName);
    }

    [Fact]
    public async Task Getting_someone_elses_draft_returns_404()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        using var desmondClient = fixture.CreateClientAs(DesmondSub);

        var id = await CreateCourseAsync(miraClient, $"Mira's Draft - 404 Probe {Guid.NewGuid()}", 99.99m, []);

        var response = await desmondClient.GetAsync($"/api/v1/courses/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_signs_shows_them_in_the_details()
    {
        using var client = fixture.CreateClientAs(MiraSub);

        var id = await CreateCourseAsync(client, $"Mira's Course - Signs Probe {Guid.NewGuid()}", 99.99m, [4, 5]); // Cancer, Leo

        var details = await GetDetailsAsync(client, id);

        Assert.Contains("Cancer", details.OpenToSignNames);
        Assert.Contains("Leo", details.OpenToSignNames);
    }

    [Fact]
    public async Task Update_replaces_the_sign_set()
    {
        using var client = fixture.CreateClientAs(MiraSub);

        var id = await CreateCourseAsync(client, $"Mira's Course - Replace Signs Probe {Guid.NewGuid()}", 99.99m, [4, 5]); // Cancer, Leo

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/courses/{id}", new UpdateCourseRequest(
            "Updated Name", "Updated description.", "Updated promise.", 99.99m, [4, 6])); // Cancer, Virgo
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var details = await GetDetailsAsync(client, id);

        Assert.Contains("Cancer", details.OpenToSignNames);
        Assert.Contains("Virgo", details.OpenToSignNames);
        Assert.DoesNotContain("Leo", details.OpenToSignNames);
    }

    [Fact]
    public async Task Update_by_a_non_author_returns_403()
    {
        using var miraClient = fixture.CreateClientAs(MiraSub);
        using var desmondClient = fixture.CreateClientAs(DesmondSub);

        // Published, not a draft: a draft is invisible (404) to a non-author before authorization
        // is even checked, so this needs a course Desmond can actually see to exercise the 403 path.
        var id = await CreateCourseAsync(miraClient, $"Mira's Course - Non-Author Probe {Guid.NewGuid()}", 99.99m, []);
        await fixture.CourseDays.CreateAsync(id, 1, "Day One", null, null, CancellationToken.None);
        await miraClient.PostAsync($"/api/v1/courses/{id}/publish", null);

        var response = await desmondClient.PutAsJsonAsync($"/api/v1/courses/{id}", new UpdateCourseRequest(
            "Hijacked", "New description.", "New promise.", 99.99m, []));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Publishing_a_course_with_no_days_returns_409()
    {
        using var client = fixture.CreateClientAs(MiraSub);

        var id = await CreateCourseAsync(client, $"Mira's Course - No Days Probe {Guid.NewGuid()}", 99.99m, []);

        var response = await client.PostAsync($"/api/v1/courses/{id}/publish", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Publishing_a_course_with_a_day_returns_204()
    {
        using var client = fixture.CreateClientAs(MiraSub);

        var id = await CreateCourseAsync(client, $"Mira's Course - Publishable Probe {Guid.NewGuid()}", 99.99m, []);

        await fixture.CourseDays.CreateAsync(id, 1, "Day One", null, null, CancellationToken.None);

        var response = await client.PostAsync($"/api/v1/courses/{id}/publish", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_course_with_cohorts_returns_409()
    {
        using var oracleClient = fixture.CreateClientAs(OracleSub);

        var listResponse = await oracleClient.GetAsync("/api/v1/courses?nameSearch=HOMEWARD&pageSize=50");
        var listResult = await listResponse.Content.ReadFromJsonAsync<PagedResult<CourseListItemDto>>(DatabaseFixture.JsonOptions);
        var homewardId = listResult!.Items.Single(c => c.Name == "HOMEWARD").Id;

        var response = await oracleClient.DeleteAsync($"/api/v1/courses/{homewardId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_fresh_draft_returns_204()
    {
        using var client = fixture.CreateClientAs(MiraSub);

        var id = await CreateCourseAsync(client, $"Mira's Course - Delete Probe {Guid.NewGuid()}", 99.99m, []);

        var response = await client.DeleteAsync($"/api/v1/courses/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<Guid> CreateCourseAsync(HttpClient client, string name, decimal price, IReadOnlyList<int> zodiacSignIds)
    {
        var response = await client.PostAsJsonAsync("/api/v1/courses", new CreateCourseCommand(
            name, "A description.", "A promise.", price, zodiacSignIds));
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location!.ToString();
        return Guid.Parse(location.Split('/').Last());
    }

    private static async Task<CourseDetailsDto> GetDetailsAsync(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"/api/v1/courses/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseDetailsDto>(DatabaseFixture.JsonOptions))!;
    }
}
