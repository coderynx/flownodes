using System.Security.Claims;
using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Devices.WriteDataPoint;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Devices.WriteDataPoint;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
                "/engine/namespaces/{namespace}/devices/{name}/datapoints/{dataPoint}",
                async (
                    [AsParameters] Request request, [FromBody] Body body, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(
                        new Command(caller.CallerId(), request.Namespace, request.Name, request.DataPoint, body.Value),
                        cancellationToken);

                    return result.Match(() => Results.NoContent(), error => error.ToProblem());
                })
            .WithName("WriteDataPoint")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    internal sealed record Request([FromRoute] string Namespace, [FromRoute] string Name, [FromRoute] string DataPoint);

    internal sealed record Body(JsonElement Value);
}
