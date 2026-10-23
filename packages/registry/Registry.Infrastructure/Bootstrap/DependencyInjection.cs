using Core.Contracts.Resources;
using JasperFx.CodeGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Registry.Application.Database;
using Registry.Infrastructure.Database;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace Registry.Infrastructure.Bootstrap;

/// <summary>
/// Adds the Registry's infrastructure to the host.
/// </summary>
public static class DependencyInjection
{
    private const string ResourceQueue = "resources";

    /// <summary>
    /// Registers the shared infrastructure, the Registry's Postgres database and the messaging that carries its
    /// Resource messages to the other modules.
    /// </summary>
    /// <param name="builder">The host the Registry is being added to.</param>
    /// <exception cref="InvalidOperationException">The <c>registry</c> connection string is not configured.</exception>
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        Core.Infrastructure.Bootstrap.DependencyInjection.AddOpenTelemetry(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHealthChecks(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHttpClientDefaults(builder);

        var connectionString = builder.Configuration.GetConnectionString("registry") ??
                               throw new InvalidOperationException("Connection string 'registry' not found.");

        AddDatabase(builder, connectionString);
        AddMessaging(builder, connectionString);

        builder.Services.AddHostedService<RegistryInitializer>();
    }

    private static void AddDatabase(IHostApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContextWithWolverineIntegration<RegistryDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IApplicationDbContext, RegistryUnitOfWork>();
    }

    private static void AddMessaging(IHostApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddWolverine(options =>
        {
            options.ServiceName = "registry";
            options.Durability.Mode = DurabilityMode.Solo;
            options.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;

            options.UsePostgresqlPersistenceAndTransport(connectionString);
            options.UseEntityFrameworkCoreTransactions();

            options.PublishMessage<ResourceApplied>().ToPostgresqlQueue(ResourceQueue);
            options.PublishMessage<ResourceDeleted>().ToPostgresqlQueue(ResourceQueue);
        });
    }
}
