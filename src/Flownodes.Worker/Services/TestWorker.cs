using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;

namespace Flownodes.Worker.Services;

public class TestWorker : BackgroundService
{
    private readonly IEnvironmentService _environmentService;
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<TestWorker> _logger;

    public TestWorker(ILogger<TestWorker> logger, IEnvironmentService environmentService, IGrainFactory grainFactory)
    {
        _logger = logger;
        _environmentService = environmentService;
        _grainFactory = grainFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tenantManager = _environmentService.GetTenantManagerGrain();
        var tenant = await tenantManager.CreateTenantAsync("default");

        var resourceManager = await tenant.GetResourceManager();

        var dictionary = new Dictionary<string, object?>
        {
            { "lightId", 1 }
        };
        var resourceConfiguration = ResourceConfigurationStore.FromDictionary(dictionary);
        resourceConfiguration.BehaviourId = "hue_light";
        await resourceManager.DeployResourceAsync<IDeviceGrain>("hue_light_1", resourceConfiguration);

        const string workflowJson = """
{
  "WorkflowName": "test_workflow",
  "Rules": [
 {
   "RuleName": "updateDeviceState",
   "Expression": "true",
   "Actions": {
      "OnSuccess": {
         "Name": "UpdateDeviceState",  
         "Context": {  
            "deviceId" : "hue_light_1",
            "deviceState": {
              "power": false
            }
         }
      }
   }
 }
  ]
}
""";

        var workflowManager = await tenant.GetWorkflowManager();
        var workflow = await workflowManager.CreateWorkflowAsync("test_workflow", workflowJson);

        await workflow.ExecuteAsync();

        /*const string code = """
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
        await scriptResource.ExecuteAsync();*/
    }
}