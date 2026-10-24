using Microsoft.AspNetCore.Builder;

namespace Identity.Infrastructure.Bootstrap;

/// <summary>
/// Prepares Identity's infrastructure on the host.
/// </summary>
public static class HostConfiguration
{
    /// <summary>
    /// Maps the shared infrastructure endpoints.
    /// </summary>
    /// <remarks>
    /// Identity migrates its database, loads or generates its signing key and logs the setup token when the host
    /// starts.
    /// </remarks>
    /// <param name="app">The host Identity is being added to.</param>
    public static void UseInfrastructure(this WebApplication app)
    {
        Core.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);
    }
}
