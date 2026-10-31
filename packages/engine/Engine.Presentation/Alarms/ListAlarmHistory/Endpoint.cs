using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Alarms.ListAlarmHistory;
using Engine.Presentation.Alarms.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Alarms.ListAlarmHistory;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/engine/namespaces/{namespace}/alarms/history",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(
                        new Query(
                            caller.CallerId(),
                            request.Namespace,
                            request.Asset,
                            request.From,
                            request.To,
                            request.Priority,
                            request.State,
                            request.Offset,
                            request.Limit),
                        cancellationToken);

                    return result.Match(
                        page => Results.Ok(new Response([.. page.Items.Select(alarm => alarm.ToDto())], page.Total)),
                        error => error.ToProblem());
                })
            .WithName("ListAlarmHistory")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Request(
        [FromRoute] string Namespace,
        [FromQuery] string? Asset,
        [FromQuery] DateTimeOffset? From,
        [FromQuery] DateTimeOffset? To,
        [FromQuery] string? Priority,
        [FromQuery] string? State,
        [FromQuery] int Offset = 0,
        [FromQuery] int Limit = 50);

    internal sealed record Response(IReadOnlyList<AlarmDto> Items, int Total);
}
