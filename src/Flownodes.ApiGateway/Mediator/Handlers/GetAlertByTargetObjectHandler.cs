using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.ApiGateway.Services;
using Flownodes.Sdk;
using Flownodes.Shared.Alerting;
using Flownodes.Shared.Tenanting;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class
    GetAlertByTargetObjectHandler : IRequestHandler<GetAlertByTargetObjectRequest, GetAlertByTargetObjectResponse>
{
    private readonly IManagersService _managersService;

    public GetAlertByTargetObjectHandler(IManagersService managersService)
    {
        _managersService = managersService;
    }

    public async Task<GetAlertByTargetObjectResponse> Handle(GetAlertByTargetObjectRequest request,
        CancellationToken cancellationToken)
    {
        var alertManager = await _managersService.GetAlertManager(request.TenantName);
        if (alertManager is null)
        {
            return new GetAlertByTargetObjectResponse(request.TenantName, request.TargetObjectName, "Tenant not found",
                ResponseKind.NotFound);
        }

        var alert = await alertManager.GetAlertByTargetObjectName(request.TargetObjectName);
        if (alert is null)
            return new GetAlertByTargetObjectResponse(request.TenantName, request.TargetObjectName, "Alert not found",
                ResponseKind.NotFound);

        try
        {
            var name = await alert.GetName();
            var state = await alert.GetState();

            return new GetAlertByTargetObjectResponse(request.TenantName, name, request.TargetObjectName,
                state.Severity, state.Description, state.FiredAt);
        }
        catch
        {
            return new GetAlertByTargetObjectResponse(request.TenantName, request.TargetObjectName,
                "Could not retrieve alert state",
                ResponseKind.InternalError);
        }
    }
}