using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Resources.ListRevisions;
using Registry.Domain.Namespaces;
using Registry.Presentation.Resources.Contracts;
using Registry.Presentation.Resources.Http;

namespace Registry.Presentation.Resources.ListRevisions;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/registry/namespaces/{namespace}/{kinds}/{name}/revisions",
                (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender, HttpContext http,
                    CancellationToken cancellationToken) => ListAsync(
                    new Query(
                        caller.CallerId(),
                        request.Namespace,
                        request.Kinds,
                        request.Name,
                        request.Offset,
                        request.Limit),
                    sender,
                    http,
                    cancellationToken))
            .WithName("ListRevisions")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet(
                "/registry/namespaces/{name}/revisions",
                (
                    [AsParameters] NamespaceRequest request, ClaimsPrincipal caller, ISender sender, HttpContext http,
                    CancellationToken cancellationToken) => ListAsync(
                    new Query(caller.CallerId(), null, Namespace.Plural, request.Name, request.Offset, request.Limit),
                    sender,
                    http,
                    cancellationToken))
            .WithName("ListNamespaceRevisions")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListAsync(
        Query query,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(query, cancellationToken);

        return result.Match(
            page => ManifestFormat.Negotiate(
                http,
                new Response([.. page.Items.Select(item => item.ToDto())], page.Total)),
            error => error.ToProblem());
    }

    internal sealed record Request(
        [FromRoute] string Namespace,
        [FromRoute] string Kinds,
        [FromRoute] string Name,
        [FromQuery] int Offset = 0,
        [FromQuery] int Limit = 50);

    internal sealed record NamespaceRequest(
        [FromRoute] string Name,
        [FromQuery] int Offset = 0,
        [FromQuery] int Limit = 50);

    internal sealed record Response(IReadOnlyList<RevisionDto> Items, int Total);
}
