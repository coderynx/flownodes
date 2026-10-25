using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Devices.InvokeAction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Devices.InvokeAction;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/engine/namespaces/{namespace}/devices/{name}/actions/{action}",
                async (
                    [AsParameters] Request request, [FromBody] Body? body, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var parameters = body?.Parameters ?? JsonSerializer.SerializeToElement(new { });
                    var result = await sender.SendAsync(
                        new Command(request.Namespace, request.Name, request.Action, parameters),
                        cancellationToken);

                    return result.Match(value => Results.Ok(new Response(value)), error => error.ToProblem());
                })
            .WithName("InvokeAction")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    internal sealed record Request([FromRoute] string Namespace, [FromRoute] string Name, [FromRoute] string Action);

    internal sealed record Body(JsonElement? Parameters);

    internal sealed record Response(JsonElement Result);
}
