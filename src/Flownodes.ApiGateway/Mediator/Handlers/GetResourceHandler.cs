using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetResourceHandler : IRequestHandler<GetResourceRequest, GetResourceResponse>
{
    public GetResourceHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>("tenant_manager");
        _resourceManager = grainFactory.GetGrain<IResourceManagerGrain>("resource_manager");
    }

    private readonly ITenantManagerGrain _tenantManager;
    private readonly IResourceManagerGrain _resourceManager;
    
    public async Task<GetResourceResponse> Handle(GetResourceRequest request, CancellationToken cancellationToken)
    {
        if (!await _tenantManager.IsTenantRegistered(request.TenantName))
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Tenant not found");

        var resource = await _resourceManager.GetGenericResourceAsync(request.TenantName, request.ResourceName);

        if (resource is null)
        {
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Resource not found");
        }
        
        return new GetResourceResponse(request.TenantName, request.ResourceName, await resource.GetMetadataProperties(),
            await resource.GetConfigurationProperties(), await resource.GetStateProperties());
    }
}