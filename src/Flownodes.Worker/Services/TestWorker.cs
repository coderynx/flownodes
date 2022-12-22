using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;

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
        var resourceManager = _environmentService.GetResourceManagerGrain();
        var alertManager = _environmentService.GetAlertManagerGrain();
        await alertManager.SetupAsync("telegram");
        // await alertManager.FireInfoAsync("resource_manager", "Started Flownodes");

        var dictionary = new Dictionary<string, object?>
        {
            { "lightId", 1 }
        };
        var resourceConfiguration = ResourceConfiguration.FromDictionary(dictionary);
        resourceConfiguration.BehaviourId = "hue_light";
        await resourceManager.DeployResourceAsync<IDeviceGrain>("hue_light_1", resourceConfiguration);

        const string code = """
            (async function () {
                    var newState = host.newObj(deviceState);
                    newState.Add('power', false);
                    var hueLight = await context.GetDevice("hue_light_1");
                    await hueLight.UpdateStateAsync(newState);
                    context.LogInformation("Script executed successfully");
            })();
      """;

        dictionary = new Dictionary<string, object?>
        {
            { "code", code }
        };
        var scriptResource = await resourceManager.DeployResourceAsync<IScriptResourceGrain>("script_01",
            ResourceConfiguration.FromDictionary(dictionary));
        await scriptResource.ExecuteAsync();

        /*var weatherConfiguration = new ResourceConfiguration
        {
            BehaviourId = "open_weather_map"
        };
        var weather =
            await resourceManager.DeployResourceAsync<IDataSourceGrain>("open_weather_1", weatherConfiguration);
        await weather.GetData("get_by_city", new Dictionary<string, object?> { { "city", "Roma" } });

        var newState = new Dictionary<string, object?>
        {
            { "power", false }
        };
        await hueLight.UpdateStateAsync(newState);

        var configuration = await hueLight.GetConfiguration();
        _logger.LogInformation("Configuration: {@Configuration}", configuration);
        var metadata = await hueLight.GetMetadata();
        _logger.LogInformation("Metadata: {Metadata}", metadata);
        var currentState = await hueLight.GetState();
        _logger.LogInformation("State: {@State}", currentState.Dictionary);

        var fritzConfiguration = new ResourceConfiguration
        {
            BehaviourId = "fritz_box"
        };
        var fritzBox = await resourceManager.DeployResourceAsync<IDeviceGrain>("fritz_box_1", fritzConfiguration);

        var state = await fritzBox.GetState();
        _logger.LogInformation("State: {State}", state.Dictionary);

        dictionary = new Dictionary<string, object?>
        {
            { "code", "context.LogInformation('Hello from ClearScript!');" }
        };
        var scriptResource = await resourceManager.DeployResourceAsync<IScriptResourceGrain>("script_resource_00",
            ResourceConfiguration.FromDictionary(dictionary));
        await scriptResource.ExecuteAsync();

        dictionary = new Dictionary<string, object?>
        {
            { "code", "context.LogInformation('Hello from ClearScript part 2!');" }
        };
        await scriptResource.UpdateConfigurationAsync(ResourceConfiguration.FromDictionary(dictionary));
        await scriptResource.ExecuteAsync();
        */
    }
}