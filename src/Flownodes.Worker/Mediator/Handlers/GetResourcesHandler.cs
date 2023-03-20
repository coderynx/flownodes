using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class GetResourcesHandler : IRequestHandler<GetResourcesRequest, GetResourcesResponse>
{
    private readonly IManagersService _managersService;

    public GetResourcesHandler(IManagersService managersService)
    {
        _managersService = managersService;
    }

    public async Task<GetResourcesResponse> Handle(GetResourcesRequest request, CancellationToken cancellationToken)
    {
        var resourceManager = await _managersService.GetResourceManager(request.TenantName);
        if (resourceManager is null)
            return new GetResourcesResponse(request.TenantName, "Tenant not found", ResponseKind.NotFound);

        try
        {
            var resources = await resourceManager.GetAllResourceSummaries();
            return new GetResourcesResponse(request.TenantName, resources.Select(x => x.Id.IdString).ToList());
        }
        catch
        {
            return new GetResourcesResponse(request.TenantName, "Could not retrieve resources",
                ResponseKind.InternalError);
        }
    }
}