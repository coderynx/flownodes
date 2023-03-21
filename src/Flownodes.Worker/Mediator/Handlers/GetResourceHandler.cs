using Flownodes.Shared.Resourcing;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class GetResourceHandler : IRequestHandler<GetResourceRequest, GetResourceResponse>
{
    private readonly IManagersService _managersService;

    public GetResourceHandler(IManagersService managersService)
    {
        _managersService = managersService;
    }

    public async Task<GetResourceResponse> Handle(GetResourceRequest request, CancellationToken cancellationToken)
    {
        var resourceManager = await _managersService.GetResourceManager(request.TenantName);
        if (resourceManager is null)
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Tenant not found",
                ResponseKind.NotFound);

        var resource = await resourceManager.GetResourceAsync(request.ResourceName);
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

            return new GetResourceResponse(id, request.TenantName, request.ResourceName, id.ToEntityKindString(),
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