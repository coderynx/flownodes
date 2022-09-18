using Orleans;
using WorkflowCore.Models;

namespace Flownodes.Edge.Core.Resources;

public interface IWorkflowManagerGrain : IGrainWithStringKey
{
    Task LoadWorkflowAsync(string workflowJson);
    Task<string?> StartWorkflowAsync(string workflowId);
    Task<WorkflowInstance?> GetInstanceAsync(string id);
}