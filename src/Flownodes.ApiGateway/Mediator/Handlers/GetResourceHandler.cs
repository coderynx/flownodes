using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Shared;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetResourceHandler : IRequestHandler<GetResourceRequest, GetResourceResponse>
{
    private readonly IResourceManagerGrain _resourceManager;

    private readonly ITenantManagerGrain _tenantManager;

    public GetResourceHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(Globals.TenantManagerName);
        _resourceManager = grainFactory.GetGrain<IResourceManagerGrain>(Globals.ResourceManagerName);
    }

    public async Task<GetResourceResponse> Handle(GetResourceRequest request, CancellationToken cancellationToken)
    {
        if (!await _tenantManager.IsTenantRegistered(request.TenantName))
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Tenant not found");

        var resource = await _resourceManager.GetGenericResourceAsync(request.TenantName, request.ResourceName);

        if (resource is null)
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Resource not found");

        var kind = await resource.GetKind();
        var fullId = await resource.GetId();
        var configuration = await resource.GetConfiguration();
        var metadata = await resource.GetMetadata();
        var state = await resource.GetState();

        return new GetResourceResponse(fullId, request.TenantName, request.ResourceName, kind, metadata.CreatedAt,
            configuration.BehaviorId, metadata.Proprties, configuration.Properties, state.Properties, state.LastUpdate);
    }
}