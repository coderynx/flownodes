using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class UpdateResourceStateHandler : IRequestHandler<UpdateResourceStateRequest, UpdateResourceStateResponse>
{
    private readonly IEnvironmentService _environmentService;

    public UpdateResourceStateHandler(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    public async Task<UpdateResourceStateResponse> Handle(UpdateResourceStateRequest request,
        CancellationToken cancellationToken)
    {
        var resourceManager = await _environmentService.GetResourceManager(request.TenantName);
        if (resourceManager is null)
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName, "Tenant not found",
                ResponseKind.NotFound);

        var grain = await resourceManager.GetResourceAsync(request.ResourceName);
        if (grain is null)
            return new UpdateResourceStateResponse(request.TenantName,
                request.ResourceName, "Resource not found",
                ResponseKind.NotFound);

        if (!await grain.GetIsStateful())
            return new UpdateResourceStateResponse(request.TenantName, request.ResourceName,
                "Cannot update state of a non stateful resource", ResponseKind.BadRequest);

        var statefulResource = grain.AsReference<IStatefulResourceGrain>();

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