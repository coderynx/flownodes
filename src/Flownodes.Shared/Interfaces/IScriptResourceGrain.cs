namespace Flownodes.Shared.Interfaces;

public interface IScriptResourceGrain : IConfigurableResource
{
    Task ExecuteAsync(Dictionary<string, object?>? parameters = null);
}