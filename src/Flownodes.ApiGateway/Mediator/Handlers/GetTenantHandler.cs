using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetTenantHandler : IRequestHandler<GetTenantRequest, GetTenantResponse>
{
    private readonly IResourceManagerGrain _resourceManager;

    private readonly ITenantManagerGrain _tenantManager;

    public GetTenantHandler(IGrainFactory clusterClient)
    {
        _tenantManager = clusterClient.GetGrain<ITenantManagerGrain>("tenant_manager");
        _resourceManager = clusterClient.GetGrain<IResourceManagerGrain>("resource_manager");
    }


    public async Task<GetTenantResponse> Handle(GetTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null) return new GetTenantResponse(request.TenantName, "Tenant not found");
        
        var resources = await _resourceManager.GetAllResourceSummaries(request.TenantName);

        return new GetTenantResponse(request.TenantName, resources.Select(x => x.Id).ToList(),
            await tenant.GetMetadata());
    }
}