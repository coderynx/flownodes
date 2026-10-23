using Core.Presentation.Bootstrap;
using Microsoft.AspNetCore.Builder;

namespace Engine.Presentation.Bootstrap;

/// <summary>
/// Maps the Engine's HTTP surface on the host.
/// </summary>
public static class HostConfiguration
{
    /// <summary>
    /// Maps the shared presentation endpoints and every endpoint the Engine declares.
    /// </summary>
    /// <param name="app">The host the Engine is being added to.</param>
    public static void UsePresentation(this WebApplication app)
    {
        Core.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);
        app.MapEndpoints(typeof(HostConfiguration).Assembly);
    }
}
