using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Sdk;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Tenanting;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class UpdateResourceStateHandler : IRequestHandler<UpdateResourceStateRequest, UpdateResourceStateResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public UpdateResourceStateHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManager);
    }

    public async Task<UpdateResourceStateResponse> Handle(UpdateResourceStateRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null)
        {
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName,"Tenant not found", ResponseKind.NotFound);
        }

        var resourceManager = await tenant.GetResourceManager();

        var grain = await resourceManager.GetGenericResourceAsync(request.ResourceName);
        if (grain is null)
            return new UpdateResourceStateResponse(request.TenantName,
                request.ResourceName, "Resource not found",
                ResponseKind.NotFound);

        if (!await grain.GetIsStateful())
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName,
                "Cannot update state of a non stateful resource", ResponseKind.BadRequest);

        var statefulResource = grain.AsReference<IStatefulResource>();

        try
        {
            await statefulResource.UpdateStateAsync(request.State);
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName, request.State);
        }
        catch
        {
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName,
                "Failed to update resource state", ResponseKind.InternalError);
        }
    }
}