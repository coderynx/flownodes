using Flownodes.Edge.Core.Resources;
using Orleans;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services.DefinitionStorage;

namespace Flownodes.Edge.Node.Resources;

public class WorkflowManagerGrain : Grain, IWorkflowManagerGrain
{
    private readonly IDefinitionLoader _definitionLoader;
    private readonly ILogger<WorkflowManagerGrain> _logger;
    private readonly IWorkflowHost _workflowHost;

    public WorkflowManagerGrain(ILogger<WorkflowManagerGrain> logger, IDefinitionLoader definitionLoader,
        IWorkflowHost workflowHost, ILifeCycleEventHub eventHub)
    {
        _logger = logger;
        _definitionLoader = definitionLoader;
        _workflowHost = workflowHost;

        _workflowHost.Start();
        eventHub.Start();
    }

    public Task LoadWorkflowAsync(string workflowJson)
    {
        var workflowDefinition = _definitionLoader.LoadDefinition(workflowJson, Deserializers.Json);

        _logger.LogInformation("Loaded workflow {WorkflowDefinitionId}", workflowDefinition.Id);
        return Task.CompletedTask;
    }

    public async Task<string?> StartWorkflowAsync(string workflowId)
    {
        var id = await _workflowHost.StartWorkflow(workflowId);

        _logger.LogInformation("Started workflow {WorkflowId}", workflowId);

        return id;
    }

    public async Task<WorkflowInstance?> GetInstanceAsync(string id)
    {
        var instance = await _workflowHost.PersistenceStore.GetWorkflowInstance(id);
        _logger.LogDebug("Retrieved workflow instance {WorkflowId}", instance.Id);

        return instance;
    }
}