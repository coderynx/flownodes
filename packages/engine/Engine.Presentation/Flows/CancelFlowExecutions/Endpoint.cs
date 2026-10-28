using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Flows.CancelFlowExecutions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Flows.CancelFlowExecutions;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/engine/namespaces/{namespace}/flows/{flow}/executions/cancel",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(
                        new Command(caller.CallerId(), request.Namespace, request.Flow),
                        cancellationToken);

                    return result.Match(
                        count => Results.Accepted(value: new Response(count)),
                        error => error.ToProblem());
                })
            .WithName("CancelFlowExecutions")
            .Produces<Response>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal sealed record Request([FromRoute] string Namespace, [FromRoute] string Flow);

    internal sealed record Response(int Cancelled);
}
