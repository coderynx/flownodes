using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.ApiGateway.Services;
using Flownodes.Sdk;
using Flownodes.Shared.Tenanting;
using Flownodes.Shared.Tenanting.Exceptions;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class CreateTenantHandler : IRequestHandler<CreateTenantRequest, CreateTenantResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public CreateTenantHandler(IManagersService managersService)
    {
        _tenantManager = managersService.GetTenantManager();
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
            return new CreateTenantResponse(exception.TenantName, exception.Message, ResponseKind.InternalError);
        }
    }
}