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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var resourceManager = _environmentService.GetResourceManagerGrain();
        
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
    }
}