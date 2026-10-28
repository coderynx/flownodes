using System.Security.Claims;
using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Flows.RunFlow;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Flows.RunFlow;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/engine/namespaces/{namespace}/flows/{flow}/run",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken, [FromBody] Body? body = null) =>
                {
                    var payload = body?.Payload ?? JsonSerializer.SerializeToElement(new { });
                    var result = await sender.SendAsync(
                        new Command(caller.CallerId(), request.Namespace, request.Flow, body?.Trigger, payload),
                        cancellationToken);

                    return result.Match(
                        run => Results.Accepted(value: new Response(run.Execution, run.Queued)),
                        error => error.ToProblem());
                })
            .WithName("RunFlow")
            .Produces<Response>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    internal sealed record Request([FromRoute] string Namespace, [FromRoute] string Flow);

    internal sealed record Body(string? Trigger, JsonElement? Payload);

    internal sealed record Response(Guid? Execution, bool Queued);
}
