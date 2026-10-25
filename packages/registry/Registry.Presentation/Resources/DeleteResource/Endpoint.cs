using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Resources.DeleteResource;
using Registry.Domain.Namespaces;

namespace Registry.Presentation.Resources.DeleteResource;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                "/registry/namespaces/{namespace}/{kinds}/{name}",
                (
                        [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                        CancellationToken cancellationToken) =>
                    DeleteAsync(
                        new Command(caller.CallerId(), request.Namespace, request.Kinds, request.Name, request.DryRun),
                        sender,
                        cancellationToken))
            .WithName("DeleteResource")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapDelete(
                "/registry/namespaces/{name}",
                (
                        [AsParameters] NamespaceRequest request, ClaimsPrincipal caller, ISender sender,
                        CancellationToken cancellationToken) =>
                    DeleteAsync(
                        new Command(caller.CallerId(), null, Namespace.Plural, request.Name, request.DryRun),
                        sender,
                        cancellationToken))
            .WithName("DeleteNamespace")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> DeleteAsync(Command command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(command, cancellationToken);

        return result.Match(_ => Results.NoContent(), error => error.ToProblem());
    }

    internal sealed record Request(
        [FromRoute] string Namespace,
        [FromRoute] string Kinds,
        [FromRoute] string Name,
        [FromQuery] bool DryRun = false);

    internal sealed record NamespaceRequest([FromRoute] string Name, [FromQuery] bool DryRun = false);
}
