using AstraKa.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AstraKa.Application;

/// <summary>
/// Composition entry point for this layer. Takes the raw MediatR license key string rather than
/// <c>Microsoft.Extensions.Configuration.IConfiguration</c> since nothing else here needs
/// deferred/lazy config resolution - unlike <c>Infrastructure.AddInfrastructure</c>, which takes
/// <c>IConfiguration</c> itself so the connection string is read lazily, after a
/// WebApplicationFactory-based test's configuration override has landed.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, string? mediatRLicenseKey = null)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

            // MediatR runs under its free Community license here; without a key it only logs a
            // startup warning (see CLAUDE.md), which is fine for this project's scale.
            if (!string.IsNullOrWhiteSpace(mediatRLicenseKey))
            {
                cfg.LicenseKey = mediatRLicenseKey;
            }
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Handlers that need "today" (e.g. Cohort's scheduling rules) take this instead of calling
        // DateTime.UtcNow directly, so a test can swap in a controllable clock.
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
