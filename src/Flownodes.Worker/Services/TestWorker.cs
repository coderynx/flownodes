using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;

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

        await tenantManager.CreateTenantAsync("default");
        
        var dictionary = new Dictionary<string, object?>
        {
            { "lightId", 1 }
        };

        var resourceConfiguration = ResourceConfigurationStore.FromDictionary(dictionary);
        resourceConfiguration.BehaviourId = "hue_light";
        await resourceManager.DeployResourceAsync<IDeviceGrain>("default", "hue_light_1", resourceConfiguration);
    }
}