using Core.Presentation.Bootstrap;
using Identity.Presentation.Connect;
using Microsoft.AspNetCore.Builder;

namespace Identity.Presentation.Bootstrap;

/// <summary>
/// Maps Identity's HTTP surface on the host.
/// </summary>
public static class HostConfiguration
{
    /// <summary>
    /// Maps the shared presentation endpoints, the sign-in pages, the token server's endpoints and every endpoint
    /// Identity declares under <c>/api/v1/identity</c>.
    /// </summary>
    /// <param name="app">The host Identity is being added to.</param>
    public static void UsePresentation(this WebApplication app)
    {
        Core.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRazorPages();
        app.MapTokenEndpoint();
        app.MapEndpoints(typeof(HostConfiguration).Assembly);
    }
}
