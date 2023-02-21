using Carter;
using Flownodes.ApiGateway.Extensions;
using Flownodes.ApiGateway.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.ApiGateway.Routes;

public class ResourceModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/tenants/{tenantName}/resources/{resourceName}",
                async ([FromServices] IMediator mediator, string tenantName, string resourceName) =>
                {
                    var request = new GetResourceRequest(tenantName, resourceName);
                    var response = await mediator.Send(request);

                    return response.GetResult();
                })
            .WithName("GetResource")
            .WithDisplayName("Get resource");

        app.MapGet("api/tenants/{tenantName}/resources", async ([FromServices] IMediator mediator, string tenantName) =>
            {
                var request = new GetResourcesRequest(tenantName);
                var response = await mediator.Send(request);

                return response.GetResult();
            })
            .WithName("GetResources")
            .WithDisplayName("Get resources");

        app.MapPut("api/tenants/{tenantName}/resources/{resourceName}",
                async ([FromServices] IMediator mediator, string tenantName, string resourceName,
                    IDictionary<string, object?> state) =>
                {
                    var request = new UpdateResourceStateRequest(tenantName, resourceName, state);
                    var response = await mediator.Send(request);

                    return response.GetResult();
                })
            .WithName("UpdateResourceState")
            .WithDisplayName("Update resource state");
    }
}