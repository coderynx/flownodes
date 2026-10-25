using System.Security.Claims;
using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Registry.Application.Resources.DeleteResources;
using Registry.Presentation.Resources.Contracts;
using Registry.Presentation.Resources.Http;

namespace Registry.Presentation.Resources.DeleteResources;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/registry/delete",
                async (
                    [AsParameters] Request request, HttpContext http, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var (documents, problem) = await ManifestFormat.ReadAsync(http.Request, cancellationToken);
                    if (documents is null)
                    {
                        return ManifestFormat.Unreadable(problem!);
                    }

                    var result = await sender.SendAsync(
                        new Command(caller.CallerId(), documents, request.Namespace, request.DryRun),
                        cancellationToken);

                    return result.Match(
                        deleted => ManifestFormat.Negotiate(
                            http,
                            new DeleteResponse(request.DryRun, [.. deleted.Select(resource => resource.ToDto())])),
                        error => error.ToResourceProblem());
                })
            .WithName("DeleteResources")
            .Accepts<JsonElement>("application/json", ManifestFormat.Yaml)
            .Produces<DeleteResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    internal sealed record Request([FromQuery] string? Namespace, [FromQuery] bool DryRun = false);
}
