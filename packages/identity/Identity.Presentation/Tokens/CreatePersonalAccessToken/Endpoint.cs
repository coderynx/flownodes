using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Identity.Application.Tokens.CreatePersonalAccessToken;
using Identity.Presentation.Tokens.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Identity.Presentation.Tokens.CreatePersonalAccessToken;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/identity/users/{id}/tokens",
                async (
                    [AsParameters] Request request, Body body, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var command = new Command(
                        caller.CallerId(),
                        caller.ResolveUser(request.Id),
                        body.Name,
                        body.ExpiresAt);
                    var result = await sender.SendAsync(command, cancellationToken);

                    return result.Match(
                        created => Results.Json(
                            created.Token.ToDto(created.Secret),
                            statusCode: StatusCodes.Status201Created),
                        error => error.ToProblem());
                })
            .WithName("CreatePersonalAccessToken")
            .RequireAuthorization()
            .Produces<TokenDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    internal sealed record Request([FromRoute] string Id);

    internal sealed record Body(string Name, DateTimeOffset? ExpiresAt);
}
