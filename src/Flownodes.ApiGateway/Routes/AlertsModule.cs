using Carter;
using Flownodes.ApiGateway.Extensions;
using Flownodes.ApiGateway.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.ApiGateway.Routes;

public class AlertsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/tenants/{tenantName}/alerts/{targetObjectName}",
                async ([FromServices] IMediator mediator, string tenantName, string targetObjectName) =>
                {
                    var request = new GetAlertByTargetObjectRequest(tenantName, targetObjectName);
                    var response = await mediator.Send(request);
                    return response.GetResult();
                })
            .WithName("GetAlertByTargetObjectRequest")
            .WithDisplayName("Get alert by target object");
    }
}