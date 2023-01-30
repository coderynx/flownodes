namespace Flownodes.Sdk.Resourcing;

/// <summary>
/// The context of the target resource in the cluster.
/// </summary>
public sealed class ResourceContext
{
    public ResourceContext(string behaviorId, Dictionary<string, object?> configuration, Dictionary<string, string> metadata,
        Dictionary<string, object?> state, DateTime? lastStateUpdate)
    {
        BehaviorId = behaviorId;
        Configuration = configuration;
        Metadata = metadata;
        State = state;
        LastStateUpdate = lastStateUpdate;
    }

    public string? BehaviorId { get; }
    public Dictionary<string, object?> Configuration { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public Dictionary<string, object?> State { get; set; }
    public DateTime? LastStateUpdate { get; }
}