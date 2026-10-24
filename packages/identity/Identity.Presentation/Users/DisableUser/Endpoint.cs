using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Identity.Application.Users.DisableUser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Identity.Presentation.Users.DisableUser;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/identity/users/{id:guid}/disable",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(new Command(caller.CallerId(), request.Id), cancellationToken);

                    return result.Match(() => Results.NoContent(), error => error.ToProblem());
                })
            .WithName("DisableUser")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    internal sealed record Request([FromRoute] Guid Id);
}
