using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Core.Infrastructure.Bootstrap;

public static class HostConfiguration
{
    private const string AppliedKey = "Core.Infrastructure.Applied";
    internal const string HealthEndpointPath = "/health";
    internal const string AlivenessEndpointPath = "/alive";

    public static void UseInfrastructure(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() || !((IApplicationBuilder)app).Properties.TryAdd(AppliedKey, true))
        {
            return;
        }

        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });
    }
}
