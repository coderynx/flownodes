using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Identity.Application.Tokens.RevokePersonalAccessToken;
using Identity.Presentation.Tokens.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Identity.Presentation.Tokens.RevokePersonalAccessToken;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                "/identity/users/{id}/tokens/{tokenId:guid}",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var command = new Command(caller.CallerId(), caller.ResolveUser(request.Id), request.TokenId);
                    var result = await sender.SendAsync(command, cancellationToken);

                    return result.Match(() => Results.NoContent(), error => error.ToProblem());
                })
            .WithName("RevokePersonalAccessToken")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Request([FromRoute] string Id, [FromRoute] Guid TokenId);
}
