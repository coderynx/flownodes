using Flownodes.Shared.Resourcing;

namespace Flownodes.Worker.Mediator.Responses;

public sealed record GetResourceResponse : Response
{
    public GetResourceResponse(string tenantName, string resourceName, IResourceSummary summary)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
        Summary = summary;
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
    public IResourceSummary? Summary { get; }
}