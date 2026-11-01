using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Web.Bff;
using Web.Session;

namespace Web.Bootstrap;

/// <summary>
/// Maps the web UI on the host.
/// </summary>
public static class HostConfiguration
{
    private const string DevServerSetting = "Flownodes:Web:DevServer";

    /// <summary>
    /// Attaches the access token of the session in a request's cookie as its bearer token, for a request with no
    /// <c>Authorization</c> header of its own. A POST, PUT, PATCH or DELETE also needs the <c>X-Flownodes-Client</c>
    /// header, which a cross-site form cannot send (ADR-0030).
    /// </summary>
    /// <remarks>
    /// Call this before the modules add authentication to the pipeline, so that they see the token.
    /// </remarks>
    /// <param name="app">The host the web UI is being added to.</param>
    public static void UseWebSession(this WebApplication app)
    {
        app.UseMiddleware<SessionBearerMiddleware>();
    }

    /// <summary>
    /// Maps the backend-for-frontend's sign-in, sign-out and session endpoints under <c>/bff</c>, and serves the web
    /// UI's build, with paths outside <c>/api</c>, <c>/bff</c> and <c>/connect</c> falling back to
    /// <c>index.html</c>.
    /// In Development, with the <c>Flownodes:Web:DevServer</c> setting present, those paths are forwarded to the Vite
    /// dev server instead, hot reload included. Endpoints the modules map keep their paths. All of it loads without a
    /// sign-in, so that the UI can start one.
    /// </summary>
    /// <param name="app">The host the web UI is being added to.</param>
    public static void UseWeb(this WebApplication app)
    {
        app.MapBffEndpoints();

        var devServer = app.Configuration[DevServerSetting];

        if (app.Environment.IsDevelopment() && devServer is not null)
        {
            app.MapForwarder($"{{*path:{ClientRouteConstraint.Name}}}", devServer).AllowAnonymous();
            return;
        }

        app.MapStaticAssets().AllowAnonymous();
        app.MapFallbackToFile($"{{*path:nonfile:{ClientRouteConstraint.Name}}}", "index.html").AllowAnonymous();
    }
}
