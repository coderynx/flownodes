using Carter;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.Worker.Routes;

public class ClusterModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/cluster", async ([FromServices] IMediator mediator) =>
            {
                var response = await mediator.Send(new GetClusterInfoRequest());
                return response.GetResult();
            })
            .AddEndpointFilter<ApiKeyEndpointFilter>()
            .WithName("GetClusterInfo")
            .WithDisplayName("Get cluster information");

        app.MapPost("api/init", [AllowAnonymous] async ([FromServices] IMediator mediator) =>
            {
                var createUserRequest = new CreateUserRequest("admin", "admin@example.com", "P@ssw0rd");
                var createUserResponse = await mediator.Send(createUserRequest);
                if (!createUserResponse.IsSuccess) return createUserResponse.GetResult();

                var generateApiKeyRequest = new CreateApiKeyRequest("admin", "default");
                var generateApiKeyResponse = await mediator.Send(generateApiKeyRequest);
                return generateApiKeyResponse.GetResult();
            })
            .WithName("InitializeCluster")
            .WithDescription("Initialize cluster");
    }
}