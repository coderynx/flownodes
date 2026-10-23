using Engine.Infrastructure.Orleans;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Dashboard;

namespace Engine.Infrastructure.Bootstrap;

/// <summary>
/// Adds the Engine's infrastructure to the host.
/// </summary>
public static class DependencyInjection
{
    private const string AdoNetInvariant = "Npgsql";

    /// <summary>
    /// Registers the shared infrastructure and makes this Silo a member of the Orleans cluster, clustered and
    /// persisted on Postgres.
    /// </summary>
    /// <param name="builder">The host the Engine is being added to.</param>
    /// <exception cref="InvalidOperationException">
    /// The clustering or grain storage connection string is not configured.
    /// </exception>
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        Core.Infrastructure.Bootstrap.DependencyInjection.AddOpenTelemetry(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHealthChecks(builder);
        Core.Infrastructure.Bootstrap.DependencyInjection.AddHttpClientDefaults(builder);

        AddOrleans(builder);
    }

    private static void AddOrleans(IHostApplicationBuilder builder)
    {
        var clusteringKey = builder.Configuration["Orleans:Clustering:ServiceKey"] ?? "engine";
        var storageKey = builder.Configuration["Orleans:GrainStorage:Default:ServiceKey"] ?? "engine";
        var clusteringConnection = builder.Configuration.GetConnectionString(clusteringKey) ??
                                   throw new InvalidOperationException(
                                       $"Connection string '{clusteringKey}' not found.");
        var storageConnection = builder.Configuration.GetConnectionString(storageKey) ??
                                throw new InvalidOperationException($"Connection string '{storageKey}' not found.");

        builder.Services.AddHostedService(_ => new OrleansSchemaInitializer(clusteringConnection));

        builder.UseOrleans(silo =>
        {
            silo.UseAdoNetClustering(options =>
            {
                options.Invariant = AdoNetInvariant;
                options.ConnectionString = clusteringConnection;
            });

            silo.AddDashboard(_ => { });

            silo.AddAdoNetGrainStorageAsDefault(options =>
            {
                options.Invariant = AdoNetInvariant;
                options.ConnectionString = storageConnection;
            });
        });
    }
}
