using Microsoft.AspNetCore.Builder;

namespace Engine.Application.Bootstrap;

public static class HostConfiguration
{
    public static void UseApplication(this WebApplication app)
    {
        Core.Application.Bootstrap.HostConfiguration.UseApplication(app);
    }
}
