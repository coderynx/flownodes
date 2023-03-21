using Flownodes.Shared.Tenanting.Grains;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class GetTenantHandler : IRequestHandler<GetTenantRequest, GetTenantResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public GetTenantHandler(IEnvironmentService environmentService)
    {
        _tenantManager = environmentService.GetTenantManager();
    }

    public async Task<GetTenantResponse> Handle(GetTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null) return new GetTenantResponse(request.TenantName, "Tenant not found", ResponseKind.NotFound);

        var resourceManager = await tenant.GetResourceManager();

        try
        {
            var resources = await resourceManager.GetAllResourceSummaries();
            return new GetTenantResponse(request.TenantName, resources.Select(x => x.Id.IdString).ToList(),
                await tenant.GetMetadata());
        }
        catch
        {
            return new GetTenantResponse(request.TenantName, "Could not retrieve tenant data",
                ResponseKind.InternalError);
        }
    }
}