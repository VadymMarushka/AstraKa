using AstraKa.Application.Common;
using AstraKa.Application.CourseProgram;
using AstraKa.Application.Reports;
using AstraKa.Infrastructure.Ado;
using AstraKa.Infrastructure.Persistence;
using AstraKa.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AstraKa.Infrastructure;

/// <summary>
/// Composition entry point for this layer, called once from AstraKa.Api's Program.cs so the Api
/// project never needs its own reference to an EF Core package.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Takes <see cref="IConfiguration"/>, not a pre-read string: Program.cs's own top-level
    /// statements run before WebApplicationBuilder.Build(), so a connection string read eagerly
    /// here would miss any override a WebApplicationFactory-based test adds via
    /// ConfigureAppConfiguration (which only lands once the host finishes building). Reading it
    /// lazily, inside the delegates below, lets the final (possibly test-overridden) configuration
    /// win.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options
            .UseSqlServer(configuration.GetConnectionString("Default"))
            .UseSeeding(TestDataSeeder.Seed)
            .UseAsyncSeeding(TestDataSeeder.SeedAsync));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ADO.NET: EF Core and the course-program repositories below share the same connection
        // string and the same physical database, per CLAUDE.md's "ADO.NET vs EF Core boundary".
        // The repositories are stateless (every method opens and disposes its own SqlConnection),
        // so a Singleton lifetime is correct - there is no per-request state to isolate.
        services.AddSingleton(_ => configuration.GetConnectionString("Default")!);
        services.AddSingleton<ICourseDayRepository, CourseDayRepository>();
        services.AddSingleton<IDayExerciseRepository, DayExerciseRepository>();
        services.AddSingleton<ICourseProgramRepository, CourseProgramRepository>();
        services.AddSingleton<IReportRepository, ReportRepository>();

        return services;
    }
}
