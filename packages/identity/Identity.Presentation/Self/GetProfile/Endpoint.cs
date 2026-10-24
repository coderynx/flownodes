using System.Security.Claims;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Presentation.Abstraction;
using Identity.Application.Self.GetProfile;
using Identity.Presentation.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Presentation.Self.GetProfile;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/identity/me",
                async (ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.SendAsync(new Query(caller.CallerId()), cancellationToken);

                    return result.Match(user => Results.Ok(user.ToDto()), error => error.ToProblem());
                })
            .WithName("GetMe")
            .RequireAuthorization()
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
