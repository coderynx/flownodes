using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class UpdateResourceStateHandler : IRequestHandler<UpdateResourceStateRequest, UpdateResourceStateResponse>
{
    public UpdateResourceStateHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>("tenant_manager");
        _resourceManager = grainFactory.GetGrain<IResourceManagerGrain>("resource_manager");
    }

    private readonly ITenantManagerGrain _tenantManager;
    private readonly IResourceManagerGrain _resourceManager;
    
    public async Task<UpdateResourceStateResponse> Handle(UpdateResourceStateRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null)
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName,"Tenant not found");

        var resource = await _resourceManager.GetGenericResourceAsync(request.TenantName, request.ResourceName);
        if (resource is null)
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName, "Resource not found");
        
        try
        {
            await resource.UpdateStateAsync((Dictionary<string, object?>) request.State);
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName, request.State);
        }
        catch
        {
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName,
                "Could not update resource state");
        }
    }
}