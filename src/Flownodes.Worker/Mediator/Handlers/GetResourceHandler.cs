using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class GetResourceHandler : IRequestHandler<GetResourceRequest, GetResourceResponse>
{
    private readonly IEnvironmentService _environmentService;

    public GetResourceHandler(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    public async Task<GetResourceResponse> Handle(GetResourceRequest request, CancellationToken cancellationToken)
    {
        var resourceManager = await _environmentService.GetResourceManager(request.TenantName);
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
                var configurableResource = resource.AsReference<IConfigurableResourceGrain>();
                configuration = await configurableResource.GetConfiguration();
            }

            Dictionary<string, object?>? state = null;
            if (!await resource.GetIsStateful())
            {
                return new GetResourceResponse(id, request.TenantName, request.ResourceName, id.ToEntityKindString(),
                    metadata, configuration, state);
                
            }
            
            var statefulResource = resource.AsReference<IStatefulResourceGrain>();
            state = await statefulResource.GetState();

            return new GetResourceResponse(id, request.TenantName, request.ResourceName, id.ToEntityKindString(),
                metadata, configuration, state);
        }
        catch
        {
            return new GetResourceResponse(request.TenantName, request.ResourceName, "Could not retrieve resource data",
                ResponseKind.InternalError);
        }
    }
}