using System.Collections.Immutable;
using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetTenantHandler : IRequestHandler<GetTenantRequest, GetTenantResponse>
{
    public GetTenantHandler(IGrainFactory clusterClient)
    {
        _tenantManager = clusterClient.GetGrain<ITenantManagerGrain>("tenant_manager");
        _resourceManager = clusterClient.GetGrain<IResourceManagerGrain>("resource_manager");
    }
    
    private readonly ITenantManagerGrain _tenantManager;
    private readonly IResourceManagerGrain _resourceManager;


    public async Task<GetTenantResponse> Handle(GetTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        var resources = await _resourceManager.GetAllResourceSummaries(request.TenantName);
        
        return new GetTenantResponse(request.TenantName, resources.Select(x => x.Id).ToList(), await tenant.GetMetadata());
    }
}