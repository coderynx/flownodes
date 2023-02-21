using Carter;
using Flownodes.ApiGateway.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.ApiGateway.Routes;

public class ResourceModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/tenants/{tenantName}/resources/{resourceName}",
                async ([FromServices] IMediator mediator, string tenantName, string resourceName) =>
                {
                    var request = new GetResourceRequest(tenantName, resourceName);
                    var response = await mediator.Send(request);

                    return response.IsSuccess is false ? Results.NotFound(response) : Results.Ok(response);
                })
            .WithName("GetResource")
            .WithDisplayName("Get resource");
    }
}