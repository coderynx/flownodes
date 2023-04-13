using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class GetResourcesHandler : IRequestHandler<GetResourcesRequest, GetResourcesResponse>
{
    private readonly IEnvironmentService _environmentService;

    public GetResourcesHandler(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    public async Task<GetResourcesResponse> Handle(GetResourcesRequest request, CancellationToken cancellationToken)
    {
        var resourceManager = await _environmentService.GetResourceManager(request.TenantName);
        if (resourceManager is null)
            return new GetResourcesResponse(request.TenantName, "Tenant not found", ResponseKind.NotFound);

        try
        {
            var resources = await resourceManager.GetAllResourceSummaries();
            return new GetResourcesResponse(request.TenantName, resources.Select(x => x.Id).ToList());
        }
        catch
        {
            return new GetResourcesResponse(request.TenantName, "Could not retrieve resources",
                ResponseKind.InternalError);
        }
    }
}