using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Alarms.ListAssetAlarms;
using Engine.Presentation.Alarms.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Alarms.ListAssetAlarms;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/engine/namespaces/{namespace}/assets/{name}/alarms",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(
                        new Query(caller.CallerId(), request.Namespace, request.Name),
                        cancellationToken);

                    return result.Match(
                        alarms => Results.Ok(new Response([.. alarms.Select(alarm => alarm.ToDto())], alarms.Count)),
                        error => error.ToProblem());
                })
            .WithName("ListAssetAlarms")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Request([FromRoute] string Namespace, [FromRoute] string Name);

    internal sealed record Response(IReadOnlyList<AlarmDto> Items, int Total);
}
