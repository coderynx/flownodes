using Flownodes.Shared.Interfaces;
using Flownodes.Worker.WorkflowActions;
using Newtonsoft.Json;
using Orleans.Runtime;
using RulesEngine.Actions;
using RulesEngine.Interfaces;
using RulesEngine.Models;

namespace Flownodes.Worker.Implementations;

[GenerateSerializer]
internal class WorkflowConfiguration
{
    [Id(0)] public string? WorkflowJson { get; set; }
}

internal class WorkflowGrain : Grain, IWorkflowGrain
{
    private readonly IPersistentState<WorkflowConfiguration> _configurationStore;

    private readonly ILogger<WorkflowGrain> _logger;
    private readonly IRulesEngine _rulesEngine;

    public WorkflowGrain(ILogger<WorkflowGrain> logger,
        [PersistentState("workflowConfigurationStore")]
        IPersistentState<WorkflowConfiguration> configurationStore, IGrainFactory grainFactory)
    {
        _logger = logger;
        _configurationStore = configurationStore;

        var tenantId = Id.Split('/')[0];
        var resourceManager = grainFactory.GetGrain<IResourceManagerGrain>(tenantId);

        // TODO: Move the settings.
        var reSettings = new ReSettings
        {
            CustomActions = new Dictionary<string, Func<ActionBase>>
            {
                { "GetDeviceState", () => new GetDeviceStateAction(resourceManager) },
                { "UpdateDeviceState", () => new UpdateDeviceStateAction(resourceManager) }
            }
        };

        _rulesEngine = new RulesEngine.RulesEngine(reSettings);
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task ConfigureAsync(string workflowJson)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowJson);

        _configurationStore.State.WorkflowJson = workflowJson;

        await _configurationStore.WriteStateAsync();
        _logger.LogInformation("Updated configuration for workflow grain {WorkflowGrainId}", Id);
    }

    public async Task ClearConfigurationAsync()
    {
        await _configurationStore.ClearStateAsync();
        _logger.LogInformation("Cleared configuration of workflow grain {WorkflowGrainId}", Id);
    }

    public async Task ExecuteAsync(IDictionary<string, object?>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(_configurationStore.State.WorkflowJson);
        var workflow = JsonConvert.DeserializeObject<Workflow>(_configurationStore.State.WorkflowJson);

        if (!_rulesEngine.ContainsWorkflow(workflow?.WorkflowName))
            _rulesEngine.AddWorkflow(workflow);

        var resultTrees = await _rulesEngine.ExecuteAllRulesAsync(workflow?.WorkflowName, parameters);

        _logger.LogInformation("Executed workflow {WorkflowGrainId} with result {@WorkflowResult}", Id, resultTrees);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated workflow grain {WorkflowGrainId}", Id);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated workflow grain {WorkflowGrainId} for reason {Reason}", Id,
            reason.Description);
        return Task.CompletedTask;
    }
}