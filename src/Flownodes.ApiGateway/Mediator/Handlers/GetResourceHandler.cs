using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Sdk;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Tenanting;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetResourceHandler : IRequestHandler<GetResourceRequest, GetResourceResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public GetResourceHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManager);
    }

    public async Task<GetResourceResponse> Handle(GetResourceRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null)
        {
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Tenant not found",
                ResponseKind.NotFound);
        }

        var resourceManager = await tenant.GetResourceManager();

        var resource = await resourceManager.GetGenericResourceAsync(request.ResourceName);
        if (resource is null)
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Resource not found",
                ResponseKind.NotFound);

        try
        {
            var id = await resource.GetId();

            var metadata = await resource.GetMetadata();

            Dictionary<string, object?>? configuration = null;
            if (await resource.GetIsConfigurable())
            {
                var configurableResource = resource.AsReference<IConfigurableResource>();
                var configurationTuple = await configurableResource.GetConfiguration();
                configuration = configurationTuple.Configuration;
            }

            (Dictionary<string, object?>? Properties, DateTime? LastUpdate) state = (null, null);
            if (await resource.GetIsStateful())
            {
                var statefulResource = resource.AsReference<IStatefulResource>();
                state = await statefulResource.GetState();
            }

            return new GetResourceResponse(id, request.TenantName, request.ResourceName, id.ToObjectKindString(),
                metadata.CreatedAtDate,
                metadata.Metadata, configuration, state.Properties, state.LastUpdate);
        }
        catch
        {
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Could not retrieve resource data",
                ResponseKind.InternalError);
        }
    }
}