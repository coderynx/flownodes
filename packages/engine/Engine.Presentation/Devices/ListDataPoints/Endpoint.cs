using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Devices.ListDataPoints;
using Engine.Presentation.Devices.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Devices.ListDataPoints;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/engine/namespaces/{namespace}/devices/{name}/datapoints",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(
                        new Query(caller.CallerId(), request.Namespace, request.Name),
                        cancellationToken);

                    return result.Match(
                        readings => Results.Ok(
                            new Response([.. readings.Select(reading => reading.ToDto())], readings.Count)),
                        error => error.ToProblem());
                })
            .WithName("ListDataPoints")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Request([FromRoute] string Namespace, [FromRoute] string Name);

    internal sealed record Response(IReadOnlyList<DataPointDto> Items, int Total);
}
