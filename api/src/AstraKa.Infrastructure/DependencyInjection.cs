using AstraKa.Application.Common;
using AstraKa.Infrastructure.Persistence;
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
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}
