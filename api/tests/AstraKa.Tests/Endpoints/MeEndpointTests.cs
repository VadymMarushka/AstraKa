using System.Net;
using System.Net.Http.Json;
using AstraKa.Application.Me;
using AstraKa.Domain.Enums;
using AstraKa.Tests.Ado;
using AstraKa.Tests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Proves the auth flow end to end against a test authentication scheme instead of real Auth0:
/// unauthenticated -> 401, authenticated-but-not-onboarded -> 404, onboarding -> 201 then a
/// matching profile, onboarding twice (or a colliding email) -> 409.
/// </summary>
[Collection("Database")]
public class MeEndpointTests(DatabaseFixture fixture)
{
    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Default"] = fixture.ConnectionString }));

            // Swap Auth0 JWT validation for the test scheme, which reads X-Test-Sub/X-Test-Email
            // headers instead of a real bearer token.
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, string? sub = null, string? email = null)
    {
        var client = factory.CreateClient();
        if (sub is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-Sub", sub);
        }

        if (email is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-Email", email);
        }

        return client;
    }

    [Fact]
    public async Task Get_me_without_a_token_returns_401()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_me_for_a_never_onboarded_sub_returns_404()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory, $"test|{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com");

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Onboarding_as_wanderer_then_get_me_returns_the_profile()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory, $"test|{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com");

        var postResponse = await client.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("Test Wanderer", 5));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/v1/me");
        var me = await getResponse.Content.ReadFromJsonAsync<MeDto>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(UserRole.Wanderer, me!.Role);
        Assert.Equal(5, me.ZodiacSignId);
    }

    [Fact]
    public async Task Onboarding_twice_for_the_same_account_returns_409()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory, $"test|{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com");

        await client.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("First Time", 3));
        var secondResponse = await client.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("Second Time", 3));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Onboarding_as_starshepherd_then_get_me_returns_the_profile()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory, $"test|{Guid.NewGuid()}", $"{Guid.NewGuid()}@example.com");

        var postResponse = await client.PostAsJsonAsync("/api/v1/me/starshepherd", new OnboardAsStarshepherdCommand("Test Shepherd", "Keeper of Tests"));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/v1/me");
        var me = await getResponse.Content.ReadFromJsonAsync<MeDto>();

        Assert.Equal(UserRole.Starshepherd, me!.Role);
        Assert.Equal("Keeper of Tests", me.Title);
    }

    [Fact]
    public async Task Onboarding_two_different_accounts_with_the_same_email_returns_409_from_the_unique_index()
    {
        using var factory = CreateFactory();
        var sharedEmail = $"{Guid.NewGuid()}@example.com";

        using var firstClient = CreateClient(factory, $"test|{Guid.NewGuid()}", sharedEmail);
        var firstResponse = await firstClient.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("First Account", 1));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var secondClient = CreateClient(factory, $"test|{Guid.NewGuid()}", sharedEmail);
        var secondResponse = await secondClient.PostAsJsonAsync("/api/v1/me/wanderer", new OnboardAsWandererCommand("Second Account", 2));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }
}
