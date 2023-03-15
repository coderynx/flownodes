using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Sdk;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetResourcesHandler : IRequestHandler<GetResourcesRequest, GetResourcesResponse>
{
    private readonly IResourceManagerGrain _resourceManager;

    private readonly ITenantManagerGrain _tenantManager;

    public GetResourcesHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManager);
        _resourceManager = grainFactory.GetGrain<IResourceManagerGrain>(FlownodesObjectNames.ResourceManager);
    }

    public async Task<GetResourcesResponse> Handle(GetResourcesRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null)
            return new GetResourcesResponse(request.TenantName, "Tenant not found", ResponseKind.NotFound);

        try
        {
            var resources = await _resourceManager.GetAllResourceSummaries(request.TenantName);
            return new GetResourcesResponse(request.TenantName, resources.Select(x => x.Id.Id).ToList());
        }
        catch
        {
            return new GetResourcesResponse(request.TenantName, "Could not retrieve resources",
                ResponseKind.InternalError);
        }
    }
}