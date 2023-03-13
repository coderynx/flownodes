using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Shared;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class
    GetAlertByTargetObjectHandler : IRequestHandler<GetAlertByTargetObjectRequest, GetAlertByTargetObjectResponse>
{
    private readonly IAlertManagerGrain _alertManager;

    private readonly ITenantManagerGrain _tenantManager;

    public GetAlertByTargetObjectHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManagerName);
        _alertManager = grainFactory.GetGrain<IAlertManagerGrain>(FlownodesObjectNames.AlertManagerName);
    }

    public async Task<GetAlertByTargetObjectResponse> Handle(GetAlertByTargetObjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _tenantManager.IsTenantRegistered(request.TenantName))
            return new GetAlertByTargetObjectResponse(request.TenantName, request.TargetObjectName, "Tenant not found",
                ResponseKind.NotFound);

        var alert = await _alertManager.GetAlertByTargetObjectName(request.TenantName, request.TargetObjectName);
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