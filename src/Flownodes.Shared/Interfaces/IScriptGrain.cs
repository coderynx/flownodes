namespace Flownodes.Shared.Interfaces;

public interface IScriptGrain : IConfigurableResource
{
    Task ExecuteAsync(Dictionary<string, object?>? parameters = null);
}