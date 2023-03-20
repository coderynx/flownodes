using Carter;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.Worker.Routes;

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