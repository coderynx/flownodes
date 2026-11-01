using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Web.Bootstrap;

/// <summary>
/// Maps the web UI on the host.
/// </summary>
public static class HostConfiguration
{
    private const string DevServerSetting = "Flownodes:Web:DevServer";

    /// <summary>
    /// Serves the web UI's build, with paths outside <c>/api</c>, <c>/bff</c> and <c>/connect</c> falling back to
    /// <c>index.html</c>.
    /// In Development, with the <c>Flownodes:Web:DevServer</c> setting present, those paths are forwarded to the Vite
    /// dev server instead, hot reload included. Endpoints the modules map keep their paths. All of it loads without a
    /// sign-in, so that the UI can start one.
    /// </summary>
    /// <param name="app">The host the web UI is being added to.</param>
    public static void UseWeb(this WebApplication app)
    {
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
