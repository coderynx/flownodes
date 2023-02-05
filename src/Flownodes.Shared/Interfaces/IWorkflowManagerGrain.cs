namespace Flownodes.Shared.Interfaces;

public interface IWorkflowManagerGrain : IGrainWithStringKey
{
    ValueTask<IWorkflowGrain?> GetWorkflowAsync(string nameOrId);
    ValueTask<IList<IWorkflowGrain>> GetWorkflowsAsync();
    ValueTask<IWorkflowGrain> CreateWorkflowAsync(string nameOrId, string workflowJson);
    Task RemoveWorkflowAsync(string nameOrId);
}