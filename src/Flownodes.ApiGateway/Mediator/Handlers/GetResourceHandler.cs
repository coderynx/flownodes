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
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(ObjectNames.TenantManagerName);
        _resourceManager = grainFactory.GetGrain<IResourceManagerGrain>(ObjectNames.ResourceManagerName);
    }

    public async Task<GetResourceResponse> Handle(GetResourceRequest request, CancellationToken cancellationToken)
    {
        if (!await _tenantManager.IsTenantRegistered(request.TenantName))
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Tenant not found",
                ResponseKind.NotFound);

        var resource = await _resourceManager.GetGenericResourceAsync(request.TenantName, request.ResourceName);

        if (resource is null)
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Resource not found",
                ResponseKind.NotFound);

        try
        {
            var kind = await resource.GetKind();
            var fullId = await resource.GetId();

            var metadata = await resource.GetMetadata();

            Dictionary<string, object?>? configuration = null;
            if (await resource.IsConfigurable())
            {
                var configurableResource = resource.AsReference<IConfigurableResource>();
                configuration = await configurableResource.GetConfiguration();
            }

            (Dictionary<string, object?>? Properties, DateTime? LastUpdate) state = (null, null);
            if (await resource.IsStateful())
            {
                var statefulResource = resource.AsReference<IStatefulResource>();
                state = await statefulResource.GetState();
            }

            return new GetResourceResponse(fullId, request.TenantName, request.ResourceName, kind, metadata.CreatedAt,
                metadata.Proprties, configuration, state.Properties, state.LastUpdate);
        }
        catch
        {
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Could not retrieve resource data",
                ResponseKind.InternalError);
        }
    }
}