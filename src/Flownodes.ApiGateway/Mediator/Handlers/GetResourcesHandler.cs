using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetResourcesHandler : IRequestHandler<GetResourcesRequest, GetResourcesResponse>
{
    public GetResourcesHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>("tenant_manager");
        _resourceManager = grainFactory.GetGrain<IResourceManagerGrain>("resource_manager");
    }

    private readonly ITenantManagerGrain _tenantManager;
    private readonly IResourceManagerGrain _resourceManager;

    public async Task<GetResourcesResponse> Handle(GetResourcesRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null) return new GetResourcesResponse(request.TenantName, "Tenant not found");
        
        var resources = await _resourceManager.GetAllResourceSummaries(request.TenantName);
        return new GetResourcesResponse(request.TenantName, resources.Select(x => x.Id).ToList());
    }
}