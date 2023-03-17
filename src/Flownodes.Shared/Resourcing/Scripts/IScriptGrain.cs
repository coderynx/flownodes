namespace Flownodes.Shared.Resourcing.Scripts;

public interface IScriptGrain : IConfigurableResource
{
    Task ExecuteAsync(Dictionary<string, object?>? parameters = null);
}