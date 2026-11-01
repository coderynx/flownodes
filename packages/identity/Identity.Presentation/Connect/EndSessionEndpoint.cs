using Identity.Presentation.Bootstrap;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using OpenIddict.Server.AspNetCore;

namespace Identity.Presentation.Connect;

internal static class EndSessionEndpoint
{
    public static void MapEndSessionEndpoint(this WebApplication app)
    {
        app.MapGet("/connect/endsession", (Delegate)HandleAsync).WithName("EndSession").AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(HttpContext http)
    {
        await http.SignOutAsync(DependencyInjection.SessionScheme);

        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }
}
