using Carter;
using Flownodes.ApiGateway.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.ApiGateway.Routes;

public class TenantModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/tenants/{tenantName}", async ([FromServices] IMediator mediator, string tenantName) =>
            {
                var request = new GetTenantRequest(tenantName);
                var response = await mediator.Send(request);
                
                return response.IsSuccess is false ? Results.NotFound(response) : Results.Ok(response);
            })
            .WithName("CreateTenant")
            .WithDisplayName("Create tenant");

        app.MapPost("/tenants", async ([FromServices] IMediator mediator, [FromBody] CreateTenantRequest request) =>
        {
            var response = await mediator.Send(request);
            
            return response.IsSuccess is false ? Results.BadRequest(response) : Results.Ok(response);
        })
            .WithName("GetTenant")
            .WithDisplayName("Get tenant");
    }
}