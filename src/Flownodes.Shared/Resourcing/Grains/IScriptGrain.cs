namespace Flownodes.Shared.Resourcing.Grains;

public interface IScriptGrain : IConfigurableResourceGrain
{
    Task ExecuteAsync(Dictionary<string, object?>? parameters = null);
}