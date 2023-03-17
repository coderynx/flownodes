namespace Flownodes.Shared.Resourcing.Scripts;

public interface IScript
{
    FlownodesContext Context { get; set; }
    Task ExecuteAsync(Dictionary<string, object?>? parameters);
}