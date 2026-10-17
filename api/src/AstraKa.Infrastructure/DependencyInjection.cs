using AstraKa.Application.Common;
using AstraKa.Application.CourseProgram;
using AstraKa.Application.Horoscopes;
using AstraKa.Application.Reports;
using AstraKa.Infrastructure.Ado;
using AstraKa.Infrastructure.Horoscopes;
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

        // Lazy bind: IOptions<T>.Value is read per-request (by the handlers), so the same
        // ConfigureAppConfiguration-override timing concern as the connection string above doesn't
        // apply here.
        services.Configure<HoroscopeOptions>(configuration.GetSection("Horoscope"));

        // Eager, startup-time branch - which IHoroscopeProvider to register has to be decided now,
        // not lazily. Fine here: unlike the connection string, no WebApplicationFactory-based test
        // ever needs this branch to see a later config override - tests swap IHoroscopeProvider
        // directly via ConfigureTestServices instead.
        if (configuration["Horoscope:Provider"] == "Fake")
        {
            services.AddSingleton<IHoroscopeProvider, FakeHoroscopeProvider>();
        }
        else
        {
            services.AddHttpClient<IHoroscopeProvider, HoroscopeProvider>(client =>
            {
                client.BaseAddress = new Uri("https://freehoroscopeapi.com/");
                client.Timeout = TimeSpan.FromSeconds(3); // short - the fallback kicks in well before a user notices
            });
        }

        return services;
    }
}
