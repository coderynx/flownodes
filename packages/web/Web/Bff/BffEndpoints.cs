using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Web.Session;

namespace Web.Bff;

internal static class BffEndpoints
{
    private const string SubjectClaim = "sub";
    private const string NameClaim = "name";

    public static void MapBffEndpoints(this WebApplication app)
    {
        var bff = app.MapGroup("/bff").AllowAnonymous();

        bff.MapGet("/signin", SignIn).WithName("BffSignIn");
        bff.MapPost("/signout", SignOutAsync).WithName("BffSignOut");
        bff.MapGet("/session", (Delegate)GetSessionAsync)
            .WithName("BffSession")
            .Produces<SessionResponse>()
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static IResult SignIn(string? returnUrl) =>
        Results.Challenge(
            new AuthenticationProperties
            {
                RedirectUri = RedirectHttpResult.IsLocalUrl(returnUrl) ? returnUrl : "/"
            },
            [SessionNames.SignInScheme]);

    private static async Task<IResult> SignOutAsync(HttpContext http, IdentityTokens tokens, CancellationToken ct)
    {
        var session = await http.AuthenticateAsync(SessionNames.SessionScheme);
        if (session.Properties?.GetTokenValue(SessionNames.RefreshToken) is { } refreshToken)
        {
            await tokens.RevokeAsync(refreshToken, ct);
        }

        await http.SignOutAsync(SessionNames.SignInScheme, new AuthenticationProperties { RedirectUri = "/" });
        await http.SignOutAsync(SessionNames.SessionScheme);

        return Results.Empty;
    }

    private static async Task<IResult> GetSessionAsync(HttpContext http)
    {
        var session = await http.AuthenticateAsync(SessionNames.SessionScheme);
        if (!session.Succeeded)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(
            new SessionResponse(
                session.Principal.FindFirst(SubjectClaim)?.Value ?? string.Empty,
                session.Principal.FindFirst(NameClaim)?.Value ?? string.Empty));
    }

    private sealed record SessionResponse(string Id, string Name);
}
