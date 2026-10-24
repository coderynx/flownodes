using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Identity.Application.Users.UpdateUser;
using Identity.Presentation.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Identity.Presentation.Users.UpdateUser;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch(
                "/identity/users/{id:guid}",
                async (
                    [AsParameters] Request request, Body body, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var command = new Command(caller.CallerId(), request.Id, body.Username, body.DisplayName);
                    var result = await sender.SendAsync(command, cancellationToken);

                    return result.Match(user => Results.Ok(user.ToDto()), error => error.ToProblem());
                })
            .WithName("UpdateUser")
            .RequireAuthorization()
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    internal sealed record Request([FromRoute] Guid Id);

    internal sealed record Body(string? Username, string? DisplayName);
}
