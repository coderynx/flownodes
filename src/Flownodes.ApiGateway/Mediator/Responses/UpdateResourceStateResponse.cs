namespace Flownodes.ApiGateway.Mediator.Responses;

public record UpdateResourceStateResponse : Response
{
    public UpdateResourceStateResponse(string tenantName, string resourceName, IDictionary<string, object?> state)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
        State = state;
    }

    public UpdateResourceStateResponse(string tenantName, string resourceName, string message,
        ResponseKind responseKind) :
        base(message, responseKind)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
    }

    public string TenantName { get; }
    public string ResourceName { get; }
    public IDictionary<string, object?>? State { get; }
}