using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Core.Infrastructure.Bootstrap;

/// <summary>
/// Maps the infrastructure endpoints every module shares.
/// </summary>
public static class HostConfiguration
{
    internal const string HealthEndpointPath = "/health";
    internal const string AlivenessEndpointPath = "/alive";
    private const string AppliedKey = "Core.Infrastructure.Applied";

    /// <summary>
    /// Maps the health and liveness endpoints, in Development only.
    /// </summary>
    /// <remarks>
    /// Every module's Infrastructure calls this, so it maps once per host and ignores later calls.
    /// </remarks>
    /// <param name="app">The host the module is being added to.</param>
    public static void UseInfrastructure(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() || !((IApplicationBuilder)app).Properties.TryAdd(AppliedKey, true))
        {
            return;
        }

        app.MapHealthChecks(HealthEndpointPath).AllowAnonymous();
        app.MapHealthChecks(
                AlivenessEndpointPath,
                new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")
                })
            .AllowAnonymous();
    }
}
