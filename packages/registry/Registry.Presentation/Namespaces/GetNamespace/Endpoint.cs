using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Namespaces.GetNamespace;
using Registry.Presentation.Namespaces.Contracts;

namespace Registry.Presentation.Namespaces.GetNamespace;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/registry/namespaces/{name}",
                async ([AsParameters] Request request, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(new Query(request.Name), cancellationToken);

                    return result.Match(@namespace => Results.Ok(@namespace.ToDto()), error => error.ToProblem());
                })
            .WithName("GetNamespace")
            .Produces<NamespaceDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Request([FromRoute] string Name);
}
