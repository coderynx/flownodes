using Engine.Infrastructure.Namespaces;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Engine.IntegrationTests;

/// <summary>
/// One Silo with the Engine's grains, in-memory storage and reminders, and a Registry that records what the Engine
/// asks of it.
/// </summary>
public sealed class EngineCluster : IAsyncDisposable
{
    public static readonly TimeSpan ReminderPeriod = TimeSpan.FromSeconds(1);
    private readonly TestCluster _cluster;

    private EngineCluster(TestCluster cluster)
    {
        _cluster = cluster;
    }

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
                });
                services.AddSingleton<RecordingRegistry>();
                services.AddSingleton<IRegistryRequests>(provider => provider.GetRequiredService<RecordingRegistry>());
            });
        }
    }
}
