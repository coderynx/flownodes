using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Namespaces.ListNamespaces;
using Registry.Presentation.Namespaces.Contracts;

namespace Registry.Presentation.Namespaces.ListNamespaces;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/registry/namespaces",
                async ([AsParameters] Request request, ISender sender, CancellationToken cancellationToken) =>
                {
                    var query = new Query(request.Q, request.Sort, request.Desc, request.Offset, request.Limit);
                    var result = await sender.SendAsync(query, cancellationToken);

                    return result.Match(
                        page => Results.Ok(new Response([.. page.Items.Select(item => item.ToDto())], page.Total)),
                        error => error.ToProblem());
                })
            .WithName("ListNamespaces")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    internal sealed record Request(
        [FromQuery] string? Q,
        [FromQuery] string? Sort,
        [FromQuery] bool Desc = false,
        [FromQuery] int Offset = 0,
        [FromQuery] int Limit = 50);

    internal sealed record Response(IReadOnlyList<NamespaceDto> Items, int Total);
}
