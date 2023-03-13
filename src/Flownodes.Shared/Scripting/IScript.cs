namespace Flownodes.Shared.Scripting;

public interface IScript
{
    FlownodesContext Context { get; set; }
    Task ExecuteAsync(Dictionary<string, object?>? parameters);
}