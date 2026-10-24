using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Successes;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Identity.Application.Grants.GrantRole;
using Identity.Presentation.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Presentation.Grants.GrantRole;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/identity/role-grants",
                async (Body body, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
                {
                    var command = new Command(caller.CallerId(), body.UserId, body.Role, body.Namespace);
                    var result = await sender.SendAsync(command, cancellationToken);

                    return result.Match(
                        user => result.Success.Kind == SuccessKind.Created
                            ? Results.Json(user.ToDto(), statusCode: StatusCodes.Status201Created)
                            : Results.Ok(user.ToDto()),
                        error => error.ToProblem());
                })
            .WithName("GrantRole")
            .RequireAuthorization()
            .Produces<UserDto>()
            .Produces<UserDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Body(Guid UserId, string? Role, string? Namespace);
}
