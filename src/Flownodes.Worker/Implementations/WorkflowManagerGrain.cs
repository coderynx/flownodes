using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Services;
using Orleans.Runtime;
using Throw;

namespace Flownodes.Worker.Implementations;

[GenerateSerializer]
internal class WorkflowManagerStore
{
    [Id(0)] public List<string> WorkflowRegistrations { get; set; } = new();
}

internal class WorkflowManagerGrain : Grain, IWorkflowManagerGrain
{
    private readonly IEnvironmentService _environmentService;
    private readonly IGrainFactory _grainFactory;

    private readonly ILogger<WorkflowManagerGrain> _logger;
    private readonly IPersistentState<WorkflowManagerStore> _store;

    public WorkflowManagerGrain(ILogger<WorkflowManagerGrain> logger,
        [PersistentState("workflowManagerStore")]
        IPersistentState<WorkflowManagerStore> store, IEnvironmentService environmentService,
        IGrainFactory grainFactory)
    {
        _logger = logger;
        _store = store;
        _environmentService = environmentService;
        _grainFactory = grainFactory;
    }

    private string Id => this.GetPrimaryKeyString();

    public ValueTask<IWorkflowGrain?> GetWorkflowAsync(string nameOrId)
    {
        nameOrId.ThrowIfNull().IfWhiteSpace().IfEmpty();
        
        if(!nameOrId.Contains('/'))
            nameOrId = GetFullId(nameOrId);

        if (!_store.State.WorkflowRegistrations.Contains(nameOrId))
        {
            _logger.LogError("The workflow {WorkflowId} does not exists", nameOrId);
            return ValueTask.FromResult<IWorkflowGrain?>(null);
        }

        var grain = _grainFactory.GetGrain<IWorkflowGrain>(nameOrId);

        _logger.LogDebug("Retrieved workflow {WorkflowId}", nameOrId);
        return ValueTask.FromResult<IWorkflowGrain?>(grain);
    }

    public async ValueTask<IList<IWorkflowGrain>> GetWorkflowsAsync()
    {
        var workflows = _store.State.WorkflowRegistrations
            .Select(registration => _grainFactory.GetGrain<IWorkflowGrain>(registration))
            .ToList();

        _logger.LogDebug("Retrieved all workflows of tenant {WorkflowManagerId}", Id);
        return workflows;
    }

    public async ValueTask<IWorkflowGrain> CreateWorkflowAsync(string nameOrId, string workflowJson)
    {
        nameOrId.ThrowIfNull().IfWhiteSpace().IfEmpty();
        workflowJson.ThrowIfNull();

        nameOrId = GetFullId(nameOrId);

        var grain = _grainFactory.GetGrain<IWorkflowGrain>(nameOrId);
        await grain.ConfigureAsync(workflowJson);

        _store.State.WorkflowRegistrations.Add(nameOrId);
        await _store.WriteStateAsync();

        _logger.LogInformation("Created workflow {WorkflowId}", nameOrId);

        return grain;
    }

    public async Task RemoveWorkflowAsync(string nameOrId)
    {
        nameOrId.ThrowIfNull().IfWhiteSpace().IfEmpty();

        if(!nameOrId.Contains('/'))
            nameOrId = GetFullId(nameOrId);

        if (!_store.State.WorkflowRegistrations.Contains(nameOrId))
        {
            _logger.LogError("The workflow {WorkflowId} does not exists", nameOrId);
            return;
        }

        var grain = _grainFactory.GetGrain<IWorkflowGrain>(nameOrId);
        await grain.ClearConfigurationAsync();

        _store.State.WorkflowRegistrations.Remove(nameOrId);
        await _store.WriteStateAsync();

        _logger.LogInformation("Removed workflow {WorkflowId}", nameOrId);
    }

    private string GetFullId(string name)
    {
        var fullId = new ObjectId(Id, _environmentService.ClusterId, name);
        return fullId.ToString();
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated workflow manager grain {WorkflowManagerGrainId}", Id);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated workflow manager grain {WorkflowManagerId} for reason {DeactivationReason}",
            Id, reason.Description);
        return Task.CompletedTask;
    }
}