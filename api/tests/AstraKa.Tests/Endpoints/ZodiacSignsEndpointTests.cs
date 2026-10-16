using System.Net.Http.Json;
using AstraKa.Application.ZodiacSigns;
using AstraKa.Tests.Ado;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AstraKa.Tests.Endpoints;

/// <summary>
/// Proves the whole skeleton end to end: controller -> MediatR -> validation behavior (no-op here,
/// the query has no validator) -> EF Core -> response, against the real app wiring in Program.cs.
/// </summary>
[Collection("Database")]
public class ZodiacSignsEndpointTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Get_zodiac_signs_returns_all_twelve_seeded_signs()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Default"] = fixture.ConnectionString })));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/zodiac-signs");
        response.EnsureSuccessStatusCode();
        var signs = await response.Content.ReadFromJsonAsync<List<ZodiacSignDto>>();

        Assert.NotNull(signs);
        Assert.Equal(12, signs!.Count);
        Assert.Contains(signs, s => s.Name == "Aries" && s.AzimuthDegrees == 0);
        Assert.Contains(signs, s => s.Name == "Libra" && s.AzimuthDegrees == 180);
    }
}
