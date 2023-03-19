using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Sdk;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Tenanting;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetResourcesHandler : IRequestHandler<GetResourcesRequest, GetResourcesResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public GetResourcesHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManager);
    }

    public async Task<GetResourcesResponse> Handle(GetResourcesRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null)
        {
            return new GetResourcesResponse(request.TenantName, "Tenant not found", ResponseKind.NotFound);
        }

        var resourceManager = await tenant.GetResourceManager();
        
        try
        {
            var resources = await resourceManager.GetAllResourceSummaries();
            return new GetResourcesResponse(request.TenantName, resources.Select(x => x.Id.IdString).ToList());
        }
        catch
        {
            return new GetResourcesResponse(request.TenantName, "Could not retrieve resources",
                ResponseKind.InternalError);
        }
    }
}