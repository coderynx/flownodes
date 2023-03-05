namespace Flownodes.ApiGateway.Mediator.Responses;

public sealed record GetResourcesResponse : Response
{
    public GetResourcesResponse(string tenantName, IReadOnlyList<string> resources)
    {
        TenantName = tenantName;
        Resources = resources;
    }

    public GetResourcesResponse(string tenantName, string message, ResponseKind responseKind) : base(message,
        responseKind)
    {
        TenantName = tenantName;
    }

    public string TenantName { get; }
    public IReadOnlyList<string>? Resources { get; }
}