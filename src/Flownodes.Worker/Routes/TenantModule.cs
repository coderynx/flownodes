using Carter;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.Worker.Routes;

public class TenantModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/tenants/{tenantName}", async ([FromServices] IMediator mediator, string tenantName) =>
            {
                var request = new GetTenantRequest(tenantName);
                var response = await mediator.Send(request);
                return response.GetResult();
            })
            .WithName("CreateTenant")
            .WithDisplayName("Create tenant");

        app.MapPost("api/tenants", async ([FromServices] IMediator mediator, [FromBody] CreateTenantRequest request) =>
            {
                var response = await mediator.Send(request);
                return response.GetResult();
            })
            .WithName("GetTenant")
            .WithDisplayName("Get tenant");
    }
}