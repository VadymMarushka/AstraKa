using AstraKa.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AstraKa.Application;

/// <summary>
/// Composition entry point for this layer. Takes the raw MediatR license key string rather than
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>, the same reason
/// <c>Infrastructure.AddInfrastructure</c> takes a raw connection string - Api already depends on
/// the configuration system and reads the value itself.
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

        return services;
    }
}
