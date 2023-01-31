namespace Flownodes.Shared.Interfaces;

public interface IWorkflowGrain : IGrainWithStringKey
{
    Task ConfigureAsync(string workflowJson);
    Task ExecuteAsync(IDictionary<string, object?>? parameters = null);
    Task ClearConfigurationAsync();
}