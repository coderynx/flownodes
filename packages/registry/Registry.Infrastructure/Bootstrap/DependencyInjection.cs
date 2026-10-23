using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Registry.Application.Database;
using Registry.Infrastructure.Database;

namespace Registry.Infrastructure.Bootstrap;

/// <summary>
/// Adds the Registry's infrastructure to the host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the shared infrastructure and the Registry's Postgres database.
    /// </summary>
    /// <param name="builder">The host the Registry is being added to.</param>
    /// <exception cref="InvalidOperationException">The <c>registry</c> connection string is not configured.</exception>
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        Core.Infrastructure.Bootstrap.DependencyInjection.AddOpenTelemetry(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHealthChecks(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHttpClientDefaults(builder);

        AddDatabase(builder);
    }

    private static void AddDatabase(IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("registry") ??
                               throw new InvalidOperationException("Connection string 'registry' not found.");

        builder.Services.AddDbContext<RegistryDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<RegistryDbContext>());
    }
}
