using Microsoft.AspNetCore.Builder;
using Orleans.Dashboard;

namespace Engine.Infrastructure.Bootstrap;

public static class HostConfiguration
{
    public static void UseInfrastructure(this WebApplication app)
    {
        Core.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);

        app.MapOrleansDashboard(routePrefix: "/orleans");
    }
}
