namespace Flownodes.ApiGateway.Mediator.Responses;

public sealed record GetResourceResponse : Response
{
    public GetResourceResponse(string fullId, string tenantName, string resourceName,
        string kind,
        DateTime createdAt,
        string? behaviorId = null,
        IDictionary<string, string?>? metadata = null,
        IDictionary<string, object?>? configuration = null,
        IDictionary<string, object?>? state = null,
        DateTime? lastStateUpdate = null)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
        FullId = fullId;
        Kind = kind;
        CreatedAt = createdAt;
        BehaviorId = behaviorId;
        Metadata = metadata;
        Configuration = configuration;
        State = state;
        LastStateUpdate = lastStateUpdate;
    }

    public GetResourceResponse(string tenantName, string resourceName, string message) : base(message)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
    }

    public string TenantName { get; }
    public string ResourceName { get; }
    public string? FullId { get; }
    public string? Kind { get; }
    public string? BehaviorId { get; }
    public IDictionary<string, string?>? Metadata { get; }
    public DateTime CreatedAt { get; }
    public IDictionary<string, object?>? Configuration { get; }
    public IDictionary<string, object?>? State { get; }
    public DateTime? LastStateUpdate { get; }
}