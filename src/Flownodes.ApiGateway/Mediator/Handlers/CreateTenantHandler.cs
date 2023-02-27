using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Shared;
using Flownodes.Shared.Exceptions;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class CreateTenantHandler : IRequestHandler<CreateTenantRequest, CreateTenantResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public CreateTenantHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(Globals.TenantManagerName);
    }

    public async Task<CreateTenantResponse> Handle(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _tenantManager.CreateTenantAsync(request.TenantName, request.Metadata);
            return new CreateTenantResponse(request.TenantName, await tenant.GetMetadata());
        }
        catch (TenantAlreadyRegisteredException exception)
        {
            return new CreateTenantResponse(exception.TenantName, exception.Message);
        }
    }
}