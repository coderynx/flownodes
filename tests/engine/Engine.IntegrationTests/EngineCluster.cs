using Engine.Application.Devices;
using Engine.Infrastructure.Connections;
using Engine.Infrastructure.Devices;
using Engine.Infrastructure.Integrations;
using Engine.Infrastructure.Namespaces;
using Engine.Infrastructure.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestingHost;

namespace Engine.IntegrationTests;

/// <summary>
/// One Silo with the Engine's grains, in-memory storage and reminders, and a Registry that records what the Engine
/// asks of it.
/// </summary>
public sealed class EngineCluster : IAsyncDisposable
{
    public static readonly Guid NamespaceUid = Guid.NewGuid();
    public static readonly TimeSpan ReminderPeriod = TimeSpan.FromSeconds(1);
    private readonly TestCluster _cluster;

    private EngineCluster(TestCluster cluster)
    {
        _cluster = cluster;
    }

    public RecordingStatus Status => _cluster.GetSiloServiceProvider().GetRequiredService<RecordingStatus>();
    public IDeviceRuntime Devices => _cluster.GetSiloServiceProvider().GetRequiredService<IDeviceRuntime>();
    public RecordingRegistry Registry => _cluster.GetSiloServiceProvider().GetRequiredService<RecordingRegistry>();

    public static async Task<EngineCluster> StartAsync(TimeSpan? resyncPeriod = null)
    {
        var builder = new TestClusterBuilder(1);
        builder.Properties["ResyncPeriod"] = (resyncPeriod ?? TimeSpan.FromMinutes(15)).ToString();
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        var cluster = builder.Build();
        await cluster.DeployAsync();

        return new EngineCluster(cluster);
    }

    /// <summary>
    /// Applies a Namespace and the Resources the Registry would publish, the way its messages reach the Engine.
    /// </summary>
    public async Task ApplyAsync(
        string @namespace,
        string kind,
        string name,
        string spec,
        long seq,
        long generation = 1)
    {
        var grain = Namespace(@namespace);
        await grain.ApplyAsync(NamespaceUid, 1);
        await grain.ResourceAppliedAsync(kind, name, ResourceUid(kind, name), generation, seq, spec);
    }

    public Task DeleteAsync(string @namespace, string kind, string name, long seq) =>
        Namespace(@namespace).ResourceDeletedAsync(kind, name, ResourceUid(kind, name), seq);

    public static Guid ResourceUid(string kind, string name) =>
        new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"{kind}/{name}")));

    internal INamespaceGrain Namespace(string name) => _cluster.GrainFactory.GetGrain<INamespaceGrain>(name);

    public async Task<bool> HasReminderAsync(string name)
    {
        var table = _cluster.GetSiloServiceProvider().GetRequiredService<IReminderTable>();
        var reminders = await table.ReadRows(Namespace(name).GetGrainId());

        return reminders.Reminders.Count == 1;
    }

    public async Task DeactivateAllAsync()
    {
        var management = _cluster.GrainFactory.GetGrain<IManagementGrain>(0);
        await management.ForceActivationCollection(TimeSpan.Zero);
    }

    public async ValueTask DisposeAsync()
    {
        await _cluster.StopAllSilosAsync();
        await _cluster.DisposeAsync();
    }

    public sealed class SiloConfigurator : ISiloConfigurator
    {
        private static CatalogOf SimulatorCatalog()
        {
            var directory = new IntegrationDirectory();
            directory.AddSimulator("simulator");
            var loads = new IntegrationLoader(NullLoggerFactory.Instance, TimeProvider.System, new StubHttpClients())
                .Load(directory.Path);

            return new CatalogOf(loads);
        }

        public void Configure(ISiloBuilder siloBuilder)
        {
            var resyncPeriod = TimeSpan.Parse(
                siloBuilder.Configuration["ResyncPeriod"]!,
                System.Globalization.CultureInfo.InvariantCulture);

            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.UseInMemoryReminderService();
            siloBuilder.ConfigureServices(services =>
            {
                services.Configure<ReminderOptions>(options => options.MinimumReminderPeriod = ReminderPeriod);
                services.Configure<NamespaceGrainOptions>(options =>
                {
                    options.ReminderPeriod = ReminderPeriod;
                    options.ResyncPeriod = resyncPeriod;
                    options.PingPeriod = TimeSpan.FromMilliseconds(200);
                });
                services.Configure<DeviceGrainOptions>(options =>
                    options.StatusInterval = TimeSpan.FromMilliseconds(50));
                services.Configure<ConnectionGrainOptions>(options =>
                {
                    options.StatusInterval = TimeSpan.FromMilliseconds(50);
                    options.InitialBackoff = TimeSpan.FromMilliseconds(100);
                    options.MaxBackoff = TimeSpan.FromMilliseconds(400);
                    options.CallTimeout = TimeSpan.FromSeconds(5);
                });
                services.AddSingleton<RecordingStatus>();
                services.AddSingleton<IStatusReporter>(provider => provider.GetRequiredService<RecordingStatus>());
                services.AddSingleton<IIntegrationCatalog>(_ => SimulatorCatalog());
                services.AddSingleton<IDeviceRuntime, GrainDeviceRuntime>();
                services.AddSingleton<RecordingRegistry>();
                services.AddSingleton<IRegistryRequests>(provider => provider.GetRequiredService<RecordingRegistry>());
            });
        }
    }

    private sealed class StubHttpClients : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class CatalogOf(IReadOnlyList<IntegrationLoad> loads) : IIntegrationCatalog
    {
        public LoadedIntegration? Find(string id) =>
            loads.Where(load => load.Installed.Id?.Value == id && load.Integration is not null)
                .Select(load => new LoadedIntegration(load.Integration!, load.Services!))
                .FirstOrDefault();
    }
}
