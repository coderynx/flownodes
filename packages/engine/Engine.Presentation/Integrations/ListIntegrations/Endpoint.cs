using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Integrations.ListIntegrations;
using Engine.Presentation.Integrations.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Integrations.ListIntegrations;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/engine/integrations",
                async (ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(new Query(caller.CallerId()), cancellationToken);

                    return result.Match(
                        integrations => Results.Ok(
                            new Response(
                                [.. integrations.Select(integration => integration.ToDto())],
                                integrations.Count)),
                        error => error.ToProblem());
                })
            .WithName("ListIntegrations")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    internal sealed record Response(IReadOnlyList<IntegrationDto> Items, int Total);
}
