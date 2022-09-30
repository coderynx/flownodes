using Flownodes.Edge.Core;
using Flownodes.Edge.Core.Resources;
using Orleans;

namespace Flownodes.Edge.Node;

public class TestWorker : BackgroundService
{
    private readonly IGrainFactory _factory;
    private readonly ILogger<TestWorker> _logger;

    public TestWorker(ILogger<TestWorker> logger, IGrainFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    private async Task FetchObbLocomotives(IDataCollectorGrain grain)
    {
        var asset = await grain.CollectAsync("get-loco",
            new Dictionary<string, object?> { { "unit_number", "8090.0744" } });
        if (asset is null) return;
    }

    private async Task SwitchOffLightAsync(IDeviceGrain grain)
    {
        await grain.PerformAction("switch-off");
        var power = grain.GetStateProperty("power").Result;
        _logger.LogInformation("Current status for HueLight {Id} is power {Power}", 1, power);
    }

    private async Task SwitchOnLightAsync(IDeviceGrain grain)
    {
        await grain.PerformAction("switch-on");
        var power = grain.GetStateProperty("power").Result;
        _logger.LogInformation("Current status for HueLight {Id} is power {Power}", 1, power);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        /*var alerter = _factory.GetGrain<IAlerterGrain>("alerter");
        await alerter.ConfigureAsync("TelegramAlerterDriver");
        await alerter.ConfigureAsync(Array.Empty<string>());
        _logger.LogInformation("Configured Telegram alerter");

        await alerter.ProduceErrorAlertAsync("frn:flownodes:node", "Test alert");*/

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var resourceManager = _factory.GetGrain<IResourceManagerGrain>(Globals.ResourceManagerGrainId);

        var weatherConfiguration = new Dictionary<string, object?>
        {
            { "latitude", "41.893333" },
            { "longitude", "12.482778" },
            { "assetNameJsonPath", "$.city" }
        };
        var weather =
            await resourceManager.RegisterDataCollectorAsync("weather", "CurrentWeatherBehavior", weatherConfiguration);

        var hueLightConfiguration = new Dictionary<string, object?>
        {
            { "lightId", 1 }
        };
        var hueLight = await resourceManager.RegisterDeviceAsync("hueLight", "HueLightBehavior", hueLightConfiguration);

        /*var lokFinderConfiguration = new Dictionary<string, object?>
        {
            { "assetNameJsonPath", "$.unit_number" }
        };
        var lokFinder =
            await resourceManager.RegisterDataCollectorAsync("lokFinder", "LokFinderBehavior", lokFinderConfiguration);*/

        while (!stoppingToken.IsCancellationRequested)
        {
            // await FetchWeatherAsync(weather);
            // await FetchObbLocomotives(lokFinder);
            // await workflowManager.StartWorkflowAsync("LoggerWorkflow");
            await SwitchOffLightAsync(hueLight);

            await Task.Delay(5000, stoppingToken);
        }
    }
}