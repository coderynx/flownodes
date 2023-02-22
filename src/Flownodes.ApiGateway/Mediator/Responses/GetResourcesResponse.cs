namespace Flownodes.ApiGateway.Mediator.Responses;

public sealed record GetResourcesResponse : Response
{
    public GetResourcesResponse(string tenantName, IReadOnlyList<string> resources)
    {
        TenantName = tenantName;
        Resources = resources;
    }

    public GetResourcesResponse(string tenantName, string message) : base(message)
    {
        TenantName = tenantName;
    }

    public string TenantName { get; }
    public IReadOnlyList<string>? Resources { get; }
}