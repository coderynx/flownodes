using FastEndpoints;
using Flownodes.Edge.ApiGateway.Services;
using Flownodes.Edge.Core.Resources;

namespace Flownodes.Edge.ApiGateway.Endpoints.GetDevice;

public class GetDeviceEndpoint : Endpoint<GetDeviceRequest>
{
    private readonly IResourceManagerGrain _resourceManager;

    public GetDeviceEndpoint(IEdgeService edgeService)
    {
        _resourceManager = edgeService.GetResourceManager();
    }

    public override void Configure()
    {
        Get("/devices");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetDeviceRequest req, CancellationToken ct)
    {
        if (req.Id is null)
            await SendErrorsAsync(cancellation: ct);

        var grain = await _resourceManager.GetDevice(req.Id!);

        if (grain is null)
            await SendNotFoundAsync(ct);

        var identityCard = await grain!.GetIdentityCard();
        var response = new GetDeviceResponse(DateTime.Now, identityCard);
        await SendAsync(response, cancellation: ct);
    }
}