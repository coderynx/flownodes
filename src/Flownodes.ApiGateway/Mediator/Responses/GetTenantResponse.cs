namespace Flownodes.ApiGateway.Mediator.Responses;

public sealed record GetTenantResponse : Response
{
    public GetTenantResponse(string tenantName, IList<string> resources, IDictionary<string, string?>? metadata = null)
    {
        TenantName = tenantName;
        Metadata = metadata;
        Resources = resources;
    }

    public GetTenantResponse(string tenantName, string message, ResponseKind responseKind) : base(message, responseKind)
    {
        TenantName = tenantName;
    }

    public string TenantName { get; }
    public IDictionary<string, string?>? Metadata { get; }
    public IList<string>? Resources { get; }
}