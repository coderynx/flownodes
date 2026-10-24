using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Namespaces.DeleteNamespace;

namespace Registry.Presentation.Namespaces.DeleteNamespace;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                "/registry/namespaces/{name}",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(
                        new Command(caller.CallerId(), request.Name),
                        cancellationToken);

                    return result.Match(() => Results.NoContent(), error => error.ToProblem());
                })
            .WithName("DeleteNamespace")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    internal sealed record Request([FromRoute] string Name);
}
