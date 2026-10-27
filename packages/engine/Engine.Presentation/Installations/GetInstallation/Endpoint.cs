using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Core.Presentation.Http;
using Engine.Application.Installations.GetInstallation;
using Engine.Presentation.Installations.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Engine.Presentation.Installations.GetInstallation;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/engine/installation",
                async (ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(new Query(caller.CallerId()), cancellationToken);

                    return result.Match(installation => Results.Ok(installation.ToDto()), error => error.ToProblem());
                })
            .WithName("GetInstallation")
            .Produces<InstallationDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
