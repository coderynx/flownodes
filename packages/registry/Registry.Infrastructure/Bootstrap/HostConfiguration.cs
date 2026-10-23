using Microsoft.AspNetCore.Builder;

namespace Registry.Infrastructure.Bootstrap;

/// <summary>
/// Prepares the Registry's infrastructure on the host.
/// </summary>
public static class HostConfiguration
{
    /// <summary>
    /// Maps the shared infrastructure endpoints.
    /// </summary>
    /// <remarks>
    /// The Registry migrates its database and seeds the <c>default</c> Namespace when the host starts, after the
    /// messaging that publishes the seed is up.
    /// </remarks>
    /// <param name="app">The host the Registry is being added to.</param>
    public static void UseInfrastructure(this WebApplication app)
    {
        Core.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);
    }
}
