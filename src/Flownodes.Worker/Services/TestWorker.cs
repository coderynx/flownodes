using Flownodes.Shared.Interfaces;

namespace Flownodes.Worker.Services;

public class TestWorker : BackgroundService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<TestWorker> _logger;

    public TestWorker(IGrainFactory grainFactory, ILogger<TestWorker> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tenantManager = _grainFactory.GetGrain<ITenantManagerGrain>("tenant_manager");
        var resourceManager = _grainFactory.GetGrain<IResourceManagerGrain>("resource_manager");
        var alertManager = _grainFactory.GetGrain<IAlertManagerGrain>("alert_manager");

        if (await tenantManager.IsTenantRegistered("default")) return;

        await tenantManager.CreateTenantAsync("default");
        var hueLightConfiguration = new Dictionary<string, object?>
        {
            { "lightId", 1 }, { "behaviourId", "hue_light" }, { "updateStateTimeSpan", 5 }
        };

        var hueLight =
            await resourceManager.DeployResourceAsync<IDeviceGrain>("default", "hue_light", hueLightConfiguration);
        var hueLightState = await hueLight.GetState();

        _logger.LogInformation("HueLight State: {@State}", hueLightState.Properties);

        var routerConfiguration = new Dictionary<string, object?>
        {
            { "behaviourId", "fritz_box" }, { "updateStateTimeSpan", 5 }
        };
        var router =
            await resourceManager.DeployResourceAsync<IDeviceGrain>("default", "fritz_box", routerConfiguration);
        var routerState = await router.GetState();

        _logger.LogInformation("Router State: {@State}", routerState.Properties);
    }
}