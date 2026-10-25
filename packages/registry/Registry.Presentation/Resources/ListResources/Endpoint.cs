using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Resources.ListResources;
using Registry.Presentation.Resources.Contracts;

namespace Registry.Presentation.Resources.ListResources;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/registry/namespaces/{namespace}/{kinds}",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender, HttpContext http,
                    CancellationToken cancellationToken) =>
                {
                    var query = new Query(
                        caller.CallerId(),
                        request.Namespace,
                        request.Kinds,
                        request.Q,
                        request.Sort,
                        request.Desc,
                        request.Offset,
                        request.Limit);
                    var result = await sender.SendAsync(query, cancellationToken);

                    return result.Match(
                        page => ManifestFormat.Negotiate(
                            http,
                            new Response([.. page.Items.Select(item => item.ToDto())], page.Total)),
                        error => error.ToProblem());
                })
            .WithName("ListResources")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Request(
        [FromRoute] string Namespace,
        [FromRoute] string Kinds,
        [FromQuery] string? Q,
        [FromQuery] string? Sort,
        [FromQuery] bool Desc = false,
        [FromQuery] int Offset = 0,
        [FromQuery] int Limit = 50);

    internal sealed record Response(IReadOnlyList<ResourceDto> Items, int Total);
}
