using Flownodes.Core.Interfaces;
using Flownodes.Core.Models;

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

    private async Task FetchObbLocomotives(IDataCollectorGrain grain)
    {
        var data = await grain.CollectAsync("get-loco",
            new Dictionary<string, object?> { { "unit_number", "8090.0744" } });
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
        var resourceManager = _environmentService.GetResourceManagerGrain();

        /*var weatherConfiguration = new Dictionary<string, object?>
        {
            { "latitude", "41.893333" },
            { "longitude", "12.482778" },
            { "assetNameJsonPath", "$.city" }
        };
        var weather =
            await resourceManager.RegisterDataCollectorAsync("weather", "CurrentWeatherBehavior", weatherConfiguration);*/

        var hueLightConfiguration = new Dictionary<string, object?>
        {
            { "lightId", 1 }
        };
        var resourceConfiguration = new ResourceConfiguration
        {
            Dictionary = hueLightConfiguration
        };

        var hueLight = await resourceManager.RegisterDeviceAsync("hueLight", "hue_light", resourceConfiguration);
        await hueLight.PerformAction("switch_off");

        var state = await hueLight.GetStateProperty("power");
        _logger.LogInformation("State: {State}", state);

        /* var lokFinderConfiguration = new Dictionary<string, object?>
         {
             { "assetNameJsonPath", "$.unit_number" }
         };
         var lokFinder =
             await resourceManager.RegisterDataCollectorAsync("lokFinder", "LokFinderBehavior", lokFinderConfiguration);
 
         while (!stoppingToken.IsCancellationRequested)
         {
             // await FetchWeatherAsync(weather);
             // await FetchObbLocomotives(lokFinder);
             // await workflowManager.StartWorkflowAsync("LoggerWorkflow");
             await SwitchOffLightAsync(hueLight);
 
             await Task.Delay(5000, stoppingToken);
         }*/
    }
}