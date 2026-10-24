using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Identity.Application.Tokens.ListPersonalAccessTokens;
using Identity.Presentation.Tokens.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Identity.Presentation.Tokens.ListPersonalAccessTokens;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/identity/users/{id}/tokens",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var query = new Query(caller.CallerId(), caller.ResolveUser(request.Id));
                    var result = await sender.SendAsync(query, cancellationToken);

                    return result.Match(
                        tokens => Results.Ok(new Response([.. tokens.Select(token => token.ToDto())])),
                        error => error.ToProblem());
                })
            .WithName("ListPersonalAccessTokens")
            .RequireAuthorization()
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    internal sealed record Request([FromRoute] string Id);

    internal sealed record Response(IReadOnlyList<TokenDto> Items);
}
