using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Successes;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Namespaces.PutNamespace;
using Registry.Presentation.Namespaces.Contracts;

namespace Registry.Presentation.Namespaces.PutNamespace;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
                "/registry/namespaces/{name}",
                async ([AsParameters] Request request, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(new Command(request.Name), cancellationToken);

                    return result.Match(
                        @namespace => result.Success.Kind == SuccessKind.Created
                            ? Results.CreatedAtRoute("GetNamespace", new { name = request.Name }, @namespace.ToDto())
                            : Results.Ok(@namespace.ToDto()),
                        error => error.ToProblem());
                })
            .WithName("PutNamespace")
            .Produces<NamespaceDto>()
            .Produces<NamespaceDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    internal sealed record Request([FromRoute] string Name);
}
