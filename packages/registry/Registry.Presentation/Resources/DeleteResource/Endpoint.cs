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
using Registry.Presentation.Resources.Contracts;
using Registry.Presentation.Resources.Http;

namespace Registry.Presentation.Resources.DeleteResource;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                "/registry/namespaces/{namespace}/{kinds}/{name}",
                (
                        [AsParameters] Request request, ClaimsPrincipal caller, ISender sender, HttpContext http,
                        CancellationToken cancellationToken) =>
                    DeleteAsync(
                        new Command(caller.CallerId(), request.Namespace, request.Kinds, request.Name, request.DryRun),
                        sender,
                        http,
                        cancellationToken))
            .WithName("DeleteResource")
            .Produces<DeleteResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapDelete(
                "/registry/namespaces/{name}",
                (
                        [AsParameters] NamespaceRequest request, ClaimsPrincipal caller, ISender sender,
                        HttpContext http,
                        CancellationToken cancellationToken) =>
                    DeleteAsync(
                        new Command(caller.CallerId(), null, Namespace.Plural, request.Name, request.DryRun),
                        sender,
                        http,
                        cancellationToken))
            .WithName("DeleteNamespace")
            .Produces<DeleteResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> DeleteAsync(
        Command command,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(command, cancellationToken);

        return result.Match(
            deleted => ManifestFormat.Negotiate(http, new DeleteResponse(command.DryRun, [deleted.ToDto()])),
            error => error.ToResourceProblem());
    }

    internal sealed record Request(
        [FromRoute] string Namespace,
        [FromRoute] string Kinds,
        [FromRoute] string Name,
        [FromQuery] bool DryRun = false);

    internal sealed record NamespaceRequest([FromRoute] string Name, [FromQuery] bool DryRun = false);
}
