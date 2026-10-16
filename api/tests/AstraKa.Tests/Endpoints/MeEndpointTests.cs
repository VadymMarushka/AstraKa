using System.Net;
using System.Net.Http.Json;
using AstraKa.Application.Me;
using AstraKa.Domain.Enums;
using AstraKa.Tests.Common;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Proves the auth flow end to end against a test authentication scheme instead of real Auth0:
/// unauthenticated -> 401, authenticated-but-not-onboarded -> 404, onboarding -> 201 then a
/// matching profile, onboarding twice (or a colliding email) -> 409.
/// </summary>
[Collection("Database")]
public class MeEndpointTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Get_me_without_a_token_returns_401()
    {
        using var client = fixture.CreateClientAs();

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_me_for_a_never_onboarded_sub_returns_404()
    {
        using var client = fixture.CreateClientAs($"test|{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com");

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Onboarding_as_wanderer_then_get_me_returns_the_profile()
    {
        using var client = fixture.CreateClientAs($"test|{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com");

        var postResponse = await client.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("Test Wanderer", 5));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/v1/me");
        var me = await getResponse.Content.ReadFromJsonAsync<MeDto>(DatabaseFixture.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(UserRole.Wanderer, me!.Role);
        Assert.Equal(5, me.ZodiacSignId);
    }

    [Fact]
    public async Task Onboarding_twice_for_the_same_account_returns_409()
    {
        using var client = fixture.CreateClientAs($"test|{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com");

        await client.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("First Time", 3));
        var secondResponse = await client.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("Second Time", 3));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Onboarding_as_starshepherd_then_get_me_returns_the_profile()
    {
        using var client = fixture.CreateClientAs($"test|{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com");

        var postResponse = await client.PostAsJsonAsync("/api/v1/me/starshepherd", new OnboardAsStarshepherdCommand("Test Shepherd", "Keeper of Tests"));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/v1/me");
        var me = await getResponse.Content.ReadFromJsonAsync<MeDto>(DatabaseFixture.JsonOptions);

        Assert.Equal(UserRole.Starshepherd, me!.Role);
        Assert.Equal("Keeper of Tests", me.Title);
    }

    [Fact]
    public async Task Onboarding_two_different_accounts_with_the_same_email_returns_409_from_the_unique_index()
    {
        var sharedEmail = $"{Guid.NewGuid()}@example.com";

        using var firstClient = fixture.CreateClientAs($"test|{Guid.NewGuid()}", sharedEmail);
        var firstResponse = await firstClient.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("First Account", 1));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var secondClient = fixture.CreateClientAs($"test|{Guid.NewGuid()}", sharedEmail);
        var secondResponse = await secondClient.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("Second Account", 2));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }
}
