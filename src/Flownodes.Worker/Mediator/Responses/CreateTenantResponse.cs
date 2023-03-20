namespace Flownodes.Worker.Mediator.Responses;

public sealed record CreateTenantResponse : Response
{
    public CreateTenantResponse(string tenantName, IDictionary<string, string?>? metadata = null)
    {
        TenantName = tenantName;
        Metadata = metadata;
    }

    public CreateTenantResponse(string tenantName, string message, ResponseKind responseKind) : base(message,
        responseKind)
    {
        TenantName = tenantName;
    }

    public string TenantName { get; }
    public IDictionary<string, string?>? Metadata { get; }
}