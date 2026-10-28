using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Flows.CancelFlowExecution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Flows.CancelFlowExecution;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/engine/namespaces/{namespace}/flows/{flow}/executions/{id:guid}/cancel",
                async (
                    [AsParameters] Request request, ClaimsPrincipal caller, ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(
                        new Command(caller.CallerId(), request.Namespace, request.Flow, request.Id),
                        cancellationToken);

                    return result.Match(() => Results.Accepted(), error => error.ToProblem());
                })
            .WithName("CancelFlowExecution")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    internal sealed record Request([FromRoute] string Namespace, [FromRoute] string Flow, [FromRoute] Guid Id);
}
