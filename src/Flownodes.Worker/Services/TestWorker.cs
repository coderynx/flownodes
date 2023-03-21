using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Scripts;

namespace Flownodes.Worker.Services;

public class TestWorker : BackgroundService
{
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger<TestWorker> _logger;

    public TestWorker(ILogger<TestWorker> logger, IEnvironmentService environmentService)
    {
        _logger = logger;
        _environmentService = environmentService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tenantManager = _environmentService.GetTenantManager();
        if (await tenantManager.IsTenantRegistered("default")) return;

        var tenant = await tenantManager.CreateTenantAsync("default");
        var resourceManager = await tenant.GetResourceManager();
        var alertManager = await tenant.GetAlertManager();

        var hueLightConfiguration = new Dictionary<string, object?>
        {
            { "lightId", 1 }, { "behaviourId", "hue_light" }, { "updateStateTimeSpan", 5 }
        };

        var hueLight = await resourceManager.DeployResourceAsync<IDeviceGrain>("hue_light", hueLightConfiguration);

        var room = await resourceManager.DeployResourceAsync<IResourceGroupGrain>("room");
        await room.RegisterResourceAsync("hue_light");

        var routerConfiguration = new Dictionary<string, object?>
        {
            { "behaviourId", "fritz_box" }, { "updateStateTimeSpan", 5 }
        };
        var router = await resourceManager.DeployResourceAsync<IDeviceGrain>("fritz_box", routerConfiguration);

        var openWeatherConfiguration = new Dictionary<string, object?>
        {
            { "behaviourId", "open_weather_map" }
        };
        var openWeather =
            await resourceManager.DeployResourceAsync<IDataSourceGrain>("open_weather_map", openWeatherConfiguration);

        const string code = """
        // #!/usr/local/bin/cscs
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Flownodes.Shared.Resourcing.Scripts;
        using Flownodes.Sdk.Alerting;

    public class TestScript : IScript
    {
        public FlownodesContext Context { get; set; }

        public async Task ExecuteAsync(Dictionary<string, object?> parameters)
        {
            Context.LogInformation("Hello");
            var state = new Dictionary<string, object?>
            {
                { "test", true }
            };
            await Context.UpdateResourceStateAsync("hue_light", state);

            await Context.CreateAlertAsync(AlertSeverity.Informational, "Hello", new HashSet<string>());
            
            var @params = new Dictionary<string, object?>
            {
                { "city", "Roma" }
            };
            var data = await Context.GetDataFromDataSourceAsync("open_weather_map", "get_current_by_city", @params);
            Context.LogInformation(data);
        }
    }
""";
        var scriptConfiguration = new Dictionary<string, object?>
        {
            { "code", code }
        };

        var script = await resourceManager.DeployResourceAsync<IScriptGrain>("script", scriptConfiguration);
        await script.ExecuteAsync();
        _logger.LogInformation("Executed script");
    }
}