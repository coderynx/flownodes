using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Sdk;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class GetTenantHandler : IRequestHandler<GetTenantRequest, GetTenantResponse>
{
    private readonly IResourceManagerGrain _resourceManager;

    private readonly ITenantManagerGrain _tenantManager;

    public GetTenantHandler(IGrainFactory clusterClient)
    {
        _tenantManager = clusterClient.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManager);
        _resourceManager = clusterClient.GetGrain<IResourceManagerGrain>(FlownodesObjectNames.ResourceManager);
    }


    public async Task<GetTenantResponse> Handle(GetTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null) return new GetTenantResponse(request.TenantName, "Tenant not found", ResponseKind.NotFound);

        try
        {
            var resources = await _resourceManager.GetAllResourceSummaries(request.TenantName);
            return new GetTenantResponse(request.TenantName, resources.Select(x => x.Id).ToList(),
                await tenant.GetMetadata());
        }
        catch
        {
            return new GetTenantResponse(request.TenantName, "Could not retrieve tenant data",
                ResponseKind.InternalError);
        }
    }
}