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
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(new Query(), cancellationToken);

                    return result.Match(
                        integrations => Results.Ok(
                            new Response(
                                [.. integrations.Select(integration => integration.ToDto())],
                                integrations.Count)),
                        error => error.ToProblem());
                })
            .WithName("ListIntegrations")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    internal sealed record Response(IReadOnlyList<IntegrationDto> Items, int Total);
}
