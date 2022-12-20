namespace Flownodes.Shared.Interfaces;

public interface IScriptResourceGrain : IResourceGrain
{
    Task ExecuteAsync(Dictionary<string, object?>? parameters = null);
}