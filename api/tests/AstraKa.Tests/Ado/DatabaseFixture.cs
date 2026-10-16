using AstraKa.Application.CourseProgram;
using AstraKa.Application.Reports;
using AstraKa.Domain.Entities;
using AstraKa.Infrastructure;
using AstraKa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AstraKa.Tests.Ado;

/// <summary>
/// Spins up a dedicated <c>AstraKa_Tests</c> LocalDB database, dropped and recreated (migrated +
/// seeded) at the start of every test run, so ADO.NET CRUD tests never accumulate stale rows across
/// runs. Shared by every test class in the "Database" collection (see <see cref="DatabaseCollection"/>).
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=AstraKa_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    private ServiceProvider? _provider;
    private IServiceScope? _scope;

    public AppDbContext DbContext { get; private set; } = null!;

    public ICourseDayRepository CourseDays { get; private set; } = null!;

    public IDayExerciseRepository DayExercises { get; private set; } = null!;

    public ICourseProgramRepository CourseProgram { get; private set; } = null!;

    public IReportRepository Reports { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(ConnectionString);
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
    }

    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
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
