using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class GetResourceGroupHandler : IRequestHandler<GetResourceGroupRequest, GetResourceGroupResponse>
{
    private readonly IEnvironmentService _environmentService;

    public GetResourceGroupHandler(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    public async Task<GetResourceGroupResponse> Handle(GetResourceGroupRequest request,
        CancellationToken cancellationToken)
    {
        var id = new FlownodesId(FlownodesEntity.ResourceGroup, request.TenantName, request.ResourceGroupName);

        var resourceManager = await _environmentService.GetResourceManager(request.TenantName);
        if (resourceManager is null)
            return new GetResourceGroupResponse(id, "Tenant not found", ResponseKind.NotFound);

        var resource = await resourceManager.GetResourceAsync<IResourceGroupGrain>(request.ResourceGroupName);
        if (resource is null)
            return new GetResourceGroupResponse(id, "Resource not found", ResponseKind.NotFound);

        try
        {
            var registrations = await resource.GetRegistrations();
            var metadata = await resource.GetMetadata();
            return new GetResourceGroupResponse(id, metadata.Metadata, registrations);
        }
        catch
        {
            return new GetResourceGroupResponse(id, "Could not retrieve resource group registrations",
                ResponseKind.BadRequest);
        }
    }
}