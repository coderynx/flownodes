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
using Registry.Application.Resources.ApplyManifest;
using Registry.Domain.Resources;
using Registry.Presentation.Resources.Http;

namespace Registry.Presentation.Resources.ApplyManifest;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/registry/apply",
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
                        applied => ManifestFormat.Negotiate(
                            http,
                            new Response(request.DryRun, [.. applied.Select(ToDto)])),
                        error => error.ToResourceProblem());
                })
            .WithName("ApplyManifest")
            .Accepts<JsonElement>("application/json", ManifestFormat.Yaml)
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static AppliedDto ToDto(AppliedResource applied) =>
        new(
            applied.Kind,
            applied.Namespace,
            applied.Name,
            applied.Action.ToString().ToLowerInvariant(),
            applied.Generation,
            [
                .. applied.Changes.Select(change => new ChangeDto(
                    change.Path,
                    Operation(change.Kind),
                    change.From,
                    change.To))
            ]);

    private static string Operation(SpecChangeKind kind) => kind.ToString().ToLowerInvariant();

    internal sealed record Request([FromQuery] string? Namespace, [FromQuery] bool DryRun = false);

    internal sealed record Response(bool DryRun, IReadOnlyList<AppliedDto> Resources);

    internal sealed record AppliedDto(
        string Kind,
        string? Namespace,
        string Name,
        string Action,
        long Generation,
        IReadOnlyList<ChangeDto> Changes);

    internal sealed record ChangeDto(string Path, string Op, JsonElement? From, JsonElement? To);
}
