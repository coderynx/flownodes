using Coderynx.MediatorKit.Abstractions;
using Identity.Application.SignIn.FindSignInUser;
using Identity.Presentation.Bootstrap;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Presentation.Connect;

internal static class AuthorizationEndpoint
{
    public static void MapAuthorizationEndpoint(this WebApplication app)
    {
        app.MapGet("/connect/authorize", HandleAsync).WithName("Authorize").AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(HttpContext http, ISender sender, CancellationToken ct)
    {
        var request = http.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var session = await http.AuthenticateAsync(DependencyInjection.SessionScheme);
        if (session.Succeeded && Guid.TryParse(session.Principal.FindFirst(Claims.Subject)?.Value, out var userId))
        {
            var found = await sender.SendAsync(new Query(userId), ct);
            if (found.IsSuccess)
            {
                return Results.SignIn(
                    TokenEndpoint.SignInPrincipal(found.Value, request.GetScopes()),
                    properties: null,
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            await http.SignOutAsync(DependencyInjection.SessionScheme);
        }

        var returnUrl = $"{http.Request.Path}{http.Request.QueryString}";

        return Results.Redirect($"/signin?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}
