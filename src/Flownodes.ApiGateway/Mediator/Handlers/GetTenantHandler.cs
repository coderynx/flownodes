using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Sdk;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Tenanting;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetTenantHandler : IRequestHandler<GetTenantRequest, GetTenantResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public GetTenantHandler(IGrainFactory clusterClient)
    {
        _tenantManager = clusterClient.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManager);
    }
    
    public async Task<GetTenantResponse> Handle(GetTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null) return new GetTenantResponse(request.TenantName, "Tenant not found", ResponseKind.NotFound);

        var resourceManager = await tenant.GetResourceManager();
        
        try
        {
            var resources = await resourceManager.GetAllResourceSummaries();
            return new GetTenantResponse(request.TenantName, resources.Select(x => x.Id.IdString).ToList(),
                await tenant.GetMetadata());
        }
        catch
        {
            return new GetTenantResponse(request.TenantName, "Could not retrieve tenant data", ResponseKind.InternalError);
        }
    }
}