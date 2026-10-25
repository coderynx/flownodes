using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Namespaces.GetNamespace;
using Registry.Presentation.Resources.Contracts;

namespace Registry.Presentation.Namespaces.GetNamespace;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/registry/namespaces/{name}",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender, HttpContext http,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(new Query(caller.CallerId(), request.Name), cancellationToken);

                    return result.Match(
                        @namespace => ManifestFormat.Negotiate(http, @namespace.ToDto()),
                        error => error.ToProblem());
                })
            .WithName("GetNamespace")
            .Produces<ResourceDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Request([FromRoute] string Name);
}
