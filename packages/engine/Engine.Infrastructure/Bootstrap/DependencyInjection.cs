using Core.Application.Integrations;
using Core.Contracts.Resources;
using Engine.Application.Devices;
using Engine.Application.Integrations;
using Engine.Infrastructure.Assets;
using Engine.Infrastructure.Connections;
using Engine.Infrastructure.Devices;
using Engine.Infrastructure.Integrations;
using Engine.Infrastructure.Namespaces;
using Engine.Infrastructure.Orleans;
using Engine.Infrastructure.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Dashboard;
using Wolverine.Postgresql;

namespace Engine.Infrastructure.Bootstrap;

/// <summary>
/// Adds the Engine's infrastructure to the host.
/// </summary>
public static class DependencyInjection
{
    private const string AdoNetInvariant = "Npgsql";
    private const string ResourceQueue = "engine-resources";

    /// <summary>
    /// Registers the shared infrastructure, makes this Silo a member of the Orleans cluster, clustered, persisted and
    /// reminded on Postgres, connects it to the Registry's Resource messages and loads the Integrations installed with
    /// the deployment from the directory in <c>Flownodes:Integrations:Directory</c>.
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
        AddMessaging(builder);
        AddIntegrations(builder);
    }

    private static void AddIntegrations(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection(IntegrationOptions.Section));
        builder.Services.AddSingleton<IntegrationLoader>();
        builder.Services.AddSingleton<InstalledIntegrations>();
        builder.Services.AddSingleton<IInstalledIntegrations>(services =>
            services.GetRequiredService<InstalledIntegrations>());
        builder.Services.AddSingleton<IIntegrationCatalog>(services =>
            services.GetRequiredService<InstalledIntegrations>());
        builder.Services.AddSingleton<IIntegrationSchemas>(services =>
            services.GetRequiredService<InstalledIntegrations>());
        builder.Services.AddHostedService(services => services.GetRequiredService<InstalledIntegrations>());
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

            silo.UseAdoNetReminderService(options =>
            {
                options.Invariant = AdoNetInvariant;
                options.ConnectionString = storageConnection;
            });
        });

        builder.Services.Configure<NamespaceGrainOptions>(
            builder.Configuration.GetSection(NamespaceGrainOptions.Section));
        builder.Services.AddSingleton<IRegistryRequests, WolverineRegistryRequests>();
        builder.Services.Configure<ConnectionGrainOptions>(
            builder.Configuration.GetSection(ConnectionGrainOptions.Section));
        builder.Services.Configure<AssetGrainOptions>(builder.Configuration.GetSection(AssetGrainOptions.Section));
        builder.Services.Configure<AssetTypeGrainOptions>(
            builder.Configuration.GetSection(AssetTypeGrainOptions.Section));
        builder.Services.Configure<DeviceGrainOptions>(builder.Configuration.GetSection(DeviceGrainOptions.Section));
        builder.Services.AddSingleton<IStatusReporter, WolverineStatusReporter>();
        builder.Services.AddSingleton<IDeviceRuntime, GrainDeviceRuntime>();
    }

    private static void AddMessaging(IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("engine") ??
                               throw new InvalidOperationException("Connection string 'engine' not found.");

        Core.Infrastructure.Bootstrap.DependencyInjection.AddMessaging(
            builder,
            connectionString,
            options =>
            {
                options.Discovery.IncludeAssembly(typeof(DependencyInjection).Assembly);

                options.PublishMessage<ResourceApplied>().ToPostgresqlQueue(ResourceQueue);
                options.PublishMessage<ResourceDeleted>().ToPostgresqlQueue(ResourceQueue);
                options.ListenToPostgresqlQueue(ResourceQueue).Sequential();
            });

        builder.Services.AddHostedService<ResyncOnStart>();
    }
}
