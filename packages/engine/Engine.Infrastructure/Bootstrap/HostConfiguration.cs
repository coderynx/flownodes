using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Orleans.Dashboard;

namespace Engine.Infrastructure.Bootstrap;

/// <summary>
/// Maps the Engine's infrastructure endpoints on the host.
/// </summary>
public static class HostConfiguration
{
    /// <summary>
    /// Maps the shared infrastructure endpoints and, in Development only, the Orleans Dashboard at <c>/orleans</c>
    /// without authentication.
    /// </summary>
    /// <param name="app">The host the Engine is being added to.</param>
    public static void UseInfrastructure(this WebApplication app)
    {
        Core.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);

        if (app.Environment.IsDevelopment())
        {
            app.MapOrleansDashboard(routePrefix: "/orleans").AllowAnonymous();
        }
    }
}
