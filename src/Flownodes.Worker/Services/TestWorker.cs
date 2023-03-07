using Flownodes.Shared.Interfaces;
using OrleansCodeGen.Orleans.Runtime;

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
        var configuration = new Dictionary<string, object?>
        {
            { "lightId", 1 }, { "behaviourId", "hue_light" }, { "updateStateTimeSpan", 5 }
        };

        var hueLight = await resourceManager.DeployResourceAsync<IDeviceGrain>("default", "hue_light_1", configuration);
        var state = await hueLight.GetState();
        _logger.LogInformation("State: {@State}", state.Properties);
    }
}