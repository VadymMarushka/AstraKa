using AstraKa.Application.Common;
using AstraKa.Application.CourseProgram;
using AstraKa.Application.Reports;
using AstraKa.Infrastructure.Ado;
using AstraKa.Infrastructure.Persistence;
using AstraKa.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AstraKa.Infrastructure;

/// <summary>
/// Composition entry point for this layer, called once from AstraKa.Api's Program.cs so the Api
/// project never needs its own reference to an EF Core package.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options
            .UseSqlServer(connectionString)
            .UseSeeding(TestDataSeeder.Seed)
            .UseAsyncSeeding(TestDataSeeder.SeedAsync));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ADO.NET: EF Core and the course-program repositories below share the same connection
        // string and the same physical database, per CLAUDE.md's "ADO.NET vs EF Core boundary".
        // The repositories are stateless (every method opens and disposes its own SqlConnection),
        // so a Singleton lifetime is correct - there is no per-request state to isolate.
        services.AddSingleton(connectionString);
        services.AddSingleton<ICourseDayRepository, CourseDayRepository>();
        services.AddSingleton<IDayExerciseRepository, DayExerciseRepository>();
        services.AddSingleton<ICourseProgramRepository, CourseProgramRepository>();
        services.AddSingleton<IReportRepository, ReportRepository>();

        return services;
    }
}
