using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Sdk;
using Flownodes.Shared.Alerting;
using Flownodes.Shared.Tenanting;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class
    GetAlertByTargetObjectHandler : IRequestHandler<GetAlertByTargetObjectRequest, GetAlertByTargetObjectResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public GetAlertByTargetObjectHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManager);
    }

    public async Task<GetAlertByTargetObjectResponse> Handle(GetAlertByTargetObjectRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null)
        {
            return new GetAlertByTargetObjectResponse(request.TenantName, request.TargetObjectName, "Tenant not found",
                ResponseKind.NotFound);
        }

        var alertManager = await tenant.GetAlertManager();
        
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