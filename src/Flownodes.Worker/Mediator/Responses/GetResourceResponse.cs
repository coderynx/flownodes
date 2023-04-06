namespace Flownodes.Worker.Mediator.Responses;

public sealed record GetResourceResponse : Response
{
    public GetResourceResponse(string fullId, string tenantName, string resourceName,
        string resourceKind,
        IDictionary<string, object?>? metadata = null,
        IDictionary<string, object?>? configuration = null,
        IDictionary<string, object?>? state = null,
        DateTime? lastStateUpdate = null)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
        FullId = fullId;
        ResourceKind = resourceKind;
        Metadata = metadata;
        Configuration = configuration;
        State = state;
        LastStateUpdate = lastStateUpdate;
    }

    public GetResourceResponse(string tenantName, string resourceName, string message, ResponseKind responseKind) :
        base(
            message, responseKind)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
    }

    public string TenantName { get; }
    public string ResourceName { get; }
    public string? FullId { get; }
    public string? ResourceKind { get; }
    public IDictionary<string, object?>? Metadata { get; }
    public IDictionary<string, object?>? Configuration { get; }
    public IDictionary<string, object?>? State { get; }
    public DateTime? LastStateUpdate { get; }
}