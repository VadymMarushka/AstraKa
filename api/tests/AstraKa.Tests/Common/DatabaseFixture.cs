using System.Text.Json;
using System.Text.Json.Serialization;
using AstraKa.Application.CourseProgram;
using AstraKa.Application.Horoscopes;
using AstraKa.Application.Reports;
using AstraKa.Domain.Entities;
using AstraKa.Infrastructure;
using AstraKa.Infrastructure.Persistence;
using AstraKa.Tests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AstraKa.Tests.Common;

/// <summary>
/// Shared by every test class in the "Database" collection (see <see cref="DatabaseCollection"/>):
/// a dedicated <c>AstraKa_Tests</c> LocalDB database, dropped and recreated (migrated + seeded) at
/// the start of every run, plus one <see cref="WebApplicationFactory{Program}"/> against it for
/// endpoint tests.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string TestConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=AstraKa_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>Matches Program.cs's own enum-as-string <c>AddJsonOptions</c> - every response read needs it too.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

    private ServiceProvider? _provider;
    private IServiceScope? _scope;
    private WebApplicationFactory<Program>? _apiFactory;

    /// <summary>For tests that need their own independent <see cref="AppDbContext"/> instances (e.g. concurrency tests).</summary>
    public string ConnectionString => TestConnectionString;

    public AppDbContext DbContext { get; private set; } = null!;

    public ICourseDayRepository CourseDays { get; private set; } = null!;

    public IDayExerciseRepository DayExercises { get; private set; } = null!;

    public ICourseProgramRepository CourseProgram { get; private set; } = null!;

    public IReportRepository Reports { get; private set; } = null!;

    /// <summary>The clock the test host's handlers see instead of the real one - defaults to real
    /// time so seeded data's relative dates stay consistent. Shared across the whole collection:
    /// any test that calls <c>SetUtcNow</c> must restore the original value in a finally block.</summary>
    public FixedTimeProvider Clock { get; private set; } = null!;

    /// <summary>Swapped in for the real/fake <see cref="IHoroscopeProvider"/> - counts calls per
    /// sign and can simulate a failure. Shared across the collection; each test should dedicate
    /// its own, never-reused sign (see the type's own doc comment).</summary>
    public CountingHoroscopeProvider Horoscope { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestConnectionString })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        _provider = services.BuildServiceProvider();

        _scope = _provider.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Every test run starts from a completely fresh database: dropped, re-migrated (schema +
        // HasData reference rows) and re-seeded (UseSeeding/UseAsyncSeeding runs as part of Migrate).
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.MigrateAsync();

        CourseDays = _provider.GetRequiredService<ICourseDayRepository>();
        DayExercises = _provider.GetRequiredService<IDayExerciseRepository>();
        CourseProgram = _provider.GetRequiredService<ICourseProgramRepository>();
        Reports = _provider.GetRequiredService<IReportRepository>();

        Clock = new FixedTimeProvider(DateTimeOffset.UtcNow);
        Horoscope = new CountingHoroscopeProvider();

        _apiFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestConnectionString }));

            // Swap Auth0 JWT validation for the test scheme, which reads X-Test-Sub/X-Test-Email
            // headers instead of a real bearer token. Swap the clock and the horoscope provider
            // too, so tests can control them.
            builder.ConfigureTestServices(apiServices =>
            {
                apiServices.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                apiServices.RemoveAll<TimeProvider>();
                apiServices.AddSingleton<TimeProvider>(Clock);

                apiServices.RemoveAll<IHoroscopeProvider>();
                apiServices.AddSingleton<IHoroscopeProvider>(Horoscope);
            });
        });

        // Fail fast: Program.cs's AddInfrastructure call reads its connection string lazily
        // specifically so a ConfigureAppConfiguration override like the one above can win - but if
        // that ever regresses (e.g. back to an eagerly-read string), every test here would silently
        // run against the dev AstraKa database instead of AstraKa_Tests. Check what the test host
        // itself actually resolves, not just what we told it to use.
        using var verificationScope = _apiFactory.Services.CreateScope();
        var effectiveConnectionString = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>().Database.GetConnectionString();
        // EF Core normalizes "Database=" to "Initial Catalog=" when echoing the connection string back.
        if (effectiveConnectionString is null || !effectiveConnectionString.Contains("Initial Catalog=AstraKa_Tests", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Test host is not using the AstraKa_Tests database - it resolved '{effectiveConnectionString}'. " +
                "The ConfigureAppConfiguration override in DatabaseFixture isn't taking effect (check that " +
                "AddInfrastructure reads its connection string lazily from IConfiguration, not eagerly as a " +
                "pre-captured string), so tests would otherwise silently run against the dev database.");
        }
    }

    public async Task DisposeAsync()
    {
        _apiFactory?.Dispose();
        _scope?.Dispose();
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
    }

    /// <summary>An HttpClient against the test Api, authenticated as <paramref name="sub"/> when given (via the test auth scheme).</summary>
    public HttpClient CreateClientAs(string? sub = null, string? email = null)
    {
        var client = _apiFactory!.CreateClient();
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

    /// <summary>A minimal Course for CRUD tests to attach CourseDays/DayExercises to, isolated from seeded data.</summary>
    public async Task<Guid> CreateThrowawayCourseAsync(string name = "Throwaway Course")
    {
        var authorId = await DbContext.Starshepherds.Select(s => s.UserId).FirstAsync();
        var course = new Course(Guid.NewGuid(), authorId, name, "Test course.", "Test promise.", 9.99m);
        DbContext.Courses.Add(course);
        await DbContext.SaveChangesAsync();
        return course.Id;
    }

    /// <summary>A stable system exercise id from the reference catalog (always present, never test-run-specific).</summary>
    public async Task<Guid> GetExerciseIdAsync(string name)
    {
        return (await DbContext.Exercises.SingleAsync(e => e.Name == name)).Id;
    }
}
