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
        var tenantManager = _environmentService.GetTenantManagerGrain();
        var tenant = await tenantManager.CreateTenantAsync("default", new Dictionary<string, string?>());
        var resourceManager = await tenant.GetResourceManager();

        var clusterGrain = _environmentService.GetClusterGrain();
        await clusterGrain.GetClusterInformation();

        var dictionary = new Dictionary<string, object?>
        {
            { "lightId", 1 }
        };
        var resourceConfiguration = ResourceConfigurationStore.FromDictionary(dictionary);
        resourceConfiguration.BehaviourId = "hue_light";
        await resourceManager.DeployResourceAsync<IDeviceGrain>("hue_light_1", resourceConfiguration);
        var hueLight = await resourceManager.GetResourceAsync<IDeviceGrain>("hue_light_1");
        var generic = await resourceManager.GetGenericResourceAsync("hue_light_1");

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
            ResourceConfigurationStore.FromDictionary(dictionary));
        await scriptResource.ExecuteAsync();
    }
}