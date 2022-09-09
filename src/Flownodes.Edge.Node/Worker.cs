using Flownodes.Edge.Core;
using Flownodes.Edge.Core.Resources;
using Orleans;

namespace Flownodes.Edge.Node;

public class Worker : BackgroundService
{
    private readonly IGrainFactory _factory;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, IGrainFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    private async Task FetchObbLocomotives(IDataCollectorGrain grain)
    {
        var asset = await grain.CollectAsync("get-loco",
            new Dictionary<string, object?> { { "unit_number", "8090.0744" } });
        if (asset is null) return;

        var frn = await asset.GetFrn();
        var data = await asset.GetData();
        _logger.LogInformation("Data of asset {AssetFrn}: {AssetData}", frn, data.ToString());
    }

    private async Task FetchWeatherAsync(IDataCollectorGrain grain)
    {
        var asset = await grain.CollectAsync(string.Empty);
        if (asset is null) return;

        var city = await asset.QueryData("$.data.city");
        var main = await asset.QueryData("$.data.main");
        var description = await asset.QueryData("$.data.description");

        _logger.LogInformation("Current sky in {CityName} is {Main} ({Description})", city?.ToString(),
            main?.ToString(),
            description?.ToString());
    }

    private async Task SwitchOffLightAsync(IDeviceGrain grain)
    {
        await grain.PerformAction("switch-off");

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

        /*var weatherConfiguration = new Dictionary<string, object?>
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
        var hueLight = await resourceManager.RegisterDeviceAsync("hueLight", "HueLightBehavior", hueLightConfiguration);*/

        var lokFinderConfiguration = new Dictionary<string, object?>
        {
            { "assetNameJsonPath", "$.unit_number" }
        };
        var lokFinder =
            await resourceManager.RegisterDataCollectorAsync("lokFinder", "LokFinderBehavior", lokFinderConfiguration);

        var workflowManager = _factory.GetGrain<IWorkflowManagerGrain>("workflow-manager");
        await workflowManager.LoadWorkflowAsync(GetTestWorkflowDefinition("LoggerWorkflow"));

        while (!stoppingToken.IsCancellationRequested)
        {
            // await FetchWeatherAsync(weather);
            await FetchObbLocomotives(lokFinder);
            await workflowManager.RunWorkflowAsync("LoggerWorkflow");
            // await SwitchOffLightAsync(hueLight);

            await Task.Delay(10000, stoppingToken);
        }
    }

    private string GetTestWorkflowDefinition(string name)
    {
        return "{\"Id\": \"" + name +
               "\",\"Version\": 1,\"Steps\": [{\"Id\": \"LogHello\",\"StepType\": \"Flownodes.Edge.Node.Automation.LoggerStep, Flownodes.Edge.Node\"}]}";
    }
}