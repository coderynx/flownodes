using Swashbuckle.AspNetCore.SwaggerGen;

namespace Flownodes.ApiGateway.Mediator.Responses;

public sealed record GetResourceResponse : Response
{
    public GetResourceResponse(string tenantName, string resourceName,
        IDictionary<string, string?>? metadata = null,
        IDictionary<string, object?>? configuration = null,
        IDictionary<string, object?> state = null)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
        Metadata = metadata;
        Configuration = configuration;
        State = state;
    }

    public GetResourceResponse(string tenantName, string resourceName, string message) : base(message)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
    }

    public string TenantName { get; }
    public string ResourceName { get; }
    public IDictionary<string, string?>? Metadata { get; }
    public IDictionary<string, object?>? Configuration { get; }
    public IDictionary<string, object?>? State { get; }
}