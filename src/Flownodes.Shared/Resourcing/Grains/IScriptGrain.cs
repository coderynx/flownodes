namespace Flownodes.Shared.Resourcing.Grains;

public interface IScriptGrain : IResourceGrain
{
    Task UpdateCodeAsync(string code);
    Task ExecuteAsync(Dictionary<string, object?>? parameters = null);
}