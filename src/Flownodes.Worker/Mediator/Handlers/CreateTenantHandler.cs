using Flownodes.Shared.Tenanting;
using Flownodes.Shared.Tenanting.Exceptions;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class CreateTenantHandler : IRequestHandler<CreateTenantRequest, CreateTenantResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public CreateTenantHandler(IEnvironmentService environmentService)
    {
        _tenantManager = environmentService.GetTenantManager();
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