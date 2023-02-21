using Carter;
using Flownodes.ApiGateway.Extensions;
using Flownodes.ApiGateway.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.ApiGateway.Routes;

public class ClusterModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/cluster", async ([FromServices] IMediator mediator) =>
            {
                var response = await mediator.Send(new GetClusterInfoRequest());
                return response.GetResult();
            })
            .WithName("GetClusterInfo")
            .WithDisplayName("Get cluster information");
    }
}