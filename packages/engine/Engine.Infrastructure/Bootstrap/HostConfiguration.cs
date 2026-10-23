using Microsoft.AspNetCore.Builder;
using Orleans.Dashboard;

namespace Engine.Infrastructure.Bootstrap;

/// <summary>
/// Maps the Engine's infrastructure endpoints on the host.
/// </summary>
public static class HostConfiguration
{
    /// <summary>
    /// Maps the shared infrastructure endpoints and the Orleans Dashboard at <c>/orleans</c>.
    /// </summary>
    /// <param name="app">The host the Engine is being added to.</param>
    public static void UseInfrastructure(this WebApplication app)
    {
        Core.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);

        app.MapOrleansDashboard(routePrefix: "/orleans");
    }
}
