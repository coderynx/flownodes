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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var resourceManager = _environmentService.GetResourceManagerGrain();

        var dictionary = new Dictionary<string, object?>
        {
            { "lightId", 1 }
        };
        var resourceConfiguration = ResourceConfiguration.FromDictionary(dictionary);

        var hueLight = await resourceManager.RegisterDeviceAsync("hue_light_1", "hue_light", resourceConfiguration);

        var newState = new Dictionary<string, object?>
        {
            { "power", false }
        };
        await hueLight.UpdateStateAsync(newState);

        var configuration = await hueLight.GetConfiguration();
        _logger.LogInformation("Metadata: {@Configuration}", configuration);
        var metadata = await hueLight.GetMetadata();
        _logger.LogInformation("Metadata: {Metadata}", metadata);
        var currentState = await hueLight.GetState();
        _logger.LogInformation("State: {State}", currentState.Dictionary);

        var fritzConfiguration = new ResourceConfiguration();
        var fritzBox = await resourceManager.RegisterDeviceAsync("fritz_box_1", "fritz_box", fritzConfiguration);

        var state = await fritzBox.GetState();
        _logger.LogInformation("State: {State}", state.Dictionary);
    }
}