using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Resources.GetResource;
using Registry.Presentation.Resources.Contracts;
using Registry.Presentation.Resources.Http;

namespace Registry.Presentation.Resources.GetResource;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/registry/namespaces/{namespace}/{kinds}/{name}",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender, HttpContext http,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(
                        new Query(caller.CallerId(), request.Namespace, request.Kinds, request.Name),
                        cancellationToken);

                    return result.Match(
                        resource => ManifestFormat.Negotiate(http, resource.ToDto()),
                        error => error.ToProblem());
                })
            .WithName("GetResource")
            .Produces<ResourceDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Request([FromRoute] string Namespace, [FromRoute] string Kinds, [FromRoute] string Name);
}
