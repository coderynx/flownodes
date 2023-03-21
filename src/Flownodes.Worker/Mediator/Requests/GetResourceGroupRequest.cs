using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public sealed record GetResourceGroupRequest : IRequest<GetResourceGroupResponse>
{
    public GetResourceGroupRequest(string tenantName, string resourceGroupName)
    {
        TenantName = tenantName;
        ResourceGroupName = resourceGroupName;
    }

    public string TenantName { get; }
    public string ResourceGroupName { get; }
}