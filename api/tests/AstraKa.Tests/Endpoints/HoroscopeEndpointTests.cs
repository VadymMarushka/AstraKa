using System.Net;
using System.Net.Http.Json;
using AstraKa.Application.Horoscopes;
using AstraKa.Application.Me;
using AstraKa.Tests.Common;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Horoscope endpoints against the fixture's <see cref="CountingHoroscopeProvider"/> - never the
/// real third-party API. Each test dedicates its own, never-reused zodiac sign (there are 12)
/// instead of resetting the shared fake provider's state, avoiding cross-test interference.
/// </summary>
[Collection("Database")]
public class HoroscopeEndpointTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Provider_failure_returns_the_fallback_text_with_200()
    {
        fixture.Horoscope.FailNextCallsFor("libra"); // signId 7

        using var client = fixture.CreateClientAs();
        var response = await client.GetAsync("/api/v1/zodiac-signs/7/horoscope");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var horoscope = await response.Content.ReadFromJsonAsync<HoroscopeDto>(DatabaseFixture.JsonOptions);
        Assert.Equal(HoroscopeFallback.Text, horoscope!.Horoscope);
    }

    [Fact]
    public async Task Second_call_for_the_same_sign_and_day_does_not_hit_the_provider_again()
    {
        using var client = fixture.CreateClientAs(); // signId 8, Scorpio

        var firstResponse = await client.GetAsync("/api/v1/zodiac-signs/8/horoscope");
        var first = await firstResponse.Content.ReadFromJsonAsync<HoroscopeDto>(DatabaseFixture.JsonOptions);

        var secondResponse = await client.GetAsync("/api/v1/zodiac-signs/8/horoscope");
        var second = await secondResponse.Content.ReadFromJsonAsync<HoroscopeDto>(DatabaseFixture.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(first!.Horoscope, second!.Horoscope);
        Assert.Equal(1, fixture.Horoscope.CallCountFor("scorpio"));
    }

    [Fact]
    public async Task My_horoscope_uses_the_Wanderers_own_sign()
    {
        using var client = await OnboardFreshWandererAsync(signId: 9); // Sagittarius

        var response = await client.GetAsync("/api/v1/me/horoscope");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var horoscope = await response.Content.ReadFromJsonAsync<HoroscopeDto>(DatabaseFixture.JsonOptions);
        Assert.Equal("Sagittarius", horoscope!.SignName);
        Assert.True(fixture.Horoscope.CallCountFor("sagittarius") >= 1);
    }

    [Fact]
    public async Task Non_onboarded_caller_gets_403_on_my_horoscope()
    {
        var sub = $"auth0|test-never-onboarded-{Guid.NewGuid()}";
        using var client = fixture.CreateClientAs(sub);

        var response = await client.GetAsync("/api/v1/me/horoscope");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_sign_id_returns_404()
    {
        using var client = fixture.CreateClientAs();

        var response = await client.GetAsync("/api/v1/zodiac-signs/999/horoscope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
}
