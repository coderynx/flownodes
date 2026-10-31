using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Web.Bootstrap;

/// <summary>
/// Adds the web UI to the host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers what serving the web UI needs.
    /// </summary>
    /// <param name="builder">The host the web UI is being added to.</param>
    public static void AddWeb(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRouting(options =>
            options.SetParameterPolicy<ClientRouteConstraint>(ClientRouteConstraint.Name));
        builder.Services.AddHttpForwarder();
    }
}
