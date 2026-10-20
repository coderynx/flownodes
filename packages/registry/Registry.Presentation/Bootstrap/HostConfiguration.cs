using Core.Presentation.Bootstrap;
using Microsoft.AspNetCore.Builder;

namespace Registry.Presentation.Bootstrap;

public static class HostConfiguration
{
    public static void UsePresentation(this WebApplication app)
    {
        Core.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);
        app.MapEndpoints(typeof(HostConfiguration).Assembly);
    }
}
