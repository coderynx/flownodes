using Flownodes.Shared.Interfaces;

namespace Flownodes.Worker.Services;

public class TestWorker : BackgroundService
{
    private readonly IGrainFactory _grainFactory;

    public TestWorker(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tenantManager = _grainFactory.GetGrain<ITenantManagerGrain>("tenant_manager");
        var resourceManager = _grainFactory.GetGrain<IResourceManagerGrain>("resource_manager");
        var alertManager = _grainFactory.GetGrain<IAlertManagerGrain>("alert_manager");

        if (await tenantManager.IsTenantRegistered("default")) return;

        await tenantManager.CreateTenantAsync("default");
        var configuration = new Dictionary<string, object?> { { "lightId", 1 }, { "behaviourId", "hue_light" } };

        var hueLight = await resourceManager.DeployResourceAsync<IDeviceGrain>("default", "hue_light_1", configuration);
    }
}