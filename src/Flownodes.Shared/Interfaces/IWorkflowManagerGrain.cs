namespace Flownodes.Shared.Interfaces;

public interface IWorkflowManagerGrain : IGrainWithStringKey
{
    ValueTask<IWorkflowGrain?> GetWorkflowAsync(string name);
    ValueTask<IList<IWorkflowGrain>> GetWorkflowsAsync();
    ValueTask<IWorkflowGrain> CreateWorkflowAsync(string name, string workflowJson);
    Task RemoveWorkflowAsync(string name);
}