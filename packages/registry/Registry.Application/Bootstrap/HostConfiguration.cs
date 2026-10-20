using Microsoft.AspNetCore.Builder;

namespace Registry.Application.Bootstrap;

public static class HostConfiguration
{
    public static void UseApplication(this WebApplication app)
    {
        Core.Application.Bootstrap.HostConfiguration.UseApplication(app);
    }
}
