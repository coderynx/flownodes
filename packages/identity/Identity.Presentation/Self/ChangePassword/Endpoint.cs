using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Identity.Application.Self.ChangePassword;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Presentation.Self.ChangePassword;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
                "/identity/me/password",
                async (Body body, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
                {
                    var command = new Command(caller.CallerId(), body.CurrentPassword, body.NewPassword);
                    var result = await sender.SendAsync(command, cancellationToken);

                    return result.Match(() => Results.NoContent(), error => error.ToProblem());
                })
            .WithName("ChangePassword")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    internal sealed record Body(string CurrentPassword, string NewPassword);
}
