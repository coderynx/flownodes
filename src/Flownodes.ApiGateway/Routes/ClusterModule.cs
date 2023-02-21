using Carter;
using Flownodes.ApiGateway.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.ApiGateway.Routes;

public class ClusterModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/cluster", async ([FromServices] IMediator mediator) =>
            {
                var response = await mediator.Send(new GetClusterInfoRequest());

                return response.IsSuccess is false ? Results.BadRequest(response) : Results.Ok(response);
            })
            .WithName("GetClusterInfo")
            .WithDisplayName("Get cluster information");
    }
}