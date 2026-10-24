using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Identity.Application.Users.CreateUser;
using Identity.Presentation.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Presentation.Users.CreateUser;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/identity/users",
                async (Body body, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
                {
                    var command = new Command(caller.CallerId(), body.Username, body.DisplayName, body.Password);
                    var result = await sender.SendAsync(command, cancellationToken);

                    return result.Match(
                        user => Results.CreatedAtRoute("ListUsers", null, user.ToDto()),
                        error => error.ToProblem());
                })
            .WithName("CreateUser")
            .RequireAuthorization()
            .Produces<UserDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    internal sealed record Body(string Username, string? DisplayName, string Password);
}
