using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Core.Infrastructure.Bootstrap;

/// <summary>
/// Registers the infrastructure every module shares: telemetry, health checks and outgoing HTTP defaults.
/// </summary>
/// <remarks>
/// Every module's Infrastructure calls each of these, so each one applies once per host and ignores later calls.
/// </remarks>
public static class DependencyInjection
{
    private const string OpenTelemetryAppliedKey = "Core.Infrastructure.OpenTelemetry.Applied";
    private const string HealthChecksAppliedKey = "Core.Infrastructure.HealthChecks.Applied";
    private const string HttpClientDefaultsAppliedKey = "Core.Infrastructure.HttpClientDefaults.Applied";

    /// <summary>
    /// Configures the HTTP client defaults: service discovery and the standard resilience handler.
    /// </summary>
    /// <param name="builder">The host the module is being added to.</param>
    public static void AddHttpClientDefaults(this IHostApplicationBuilder builder)
    {
        if (!builder.Properties.TryAdd(HttpClientDefaultsAppliedKey, true))
        {
            return;
        }

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
    }

    /// <summary>
    /// Configures OpenTelemetry logging, metrics and tracing, exported over OTLP when an endpoint is configured.
    /// </summary>
    /// <param name="builder">The host the module is being added to.</param>
    public static void AddOpenTelemetry(this IHostApplicationBuilder builder)
    {
        if (!builder.Properties.TryAdd(OpenTelemetryAppliedKey, true))
        {
            return;
        }

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing.AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                    !context.Request.Path.StartsWithSegments(HostConfiguration.HealthEndpointPath) &&
                    !context.Request.Path.StartsWithSegments(HostConfiguration.AlivenessEndpointPath))
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }

    /// <summary>
    /// Registers the liveness health check.
    /// </summary>
    /// <param name="builder">The host the module is being added to.</param>
    public static void AddHealthChecks(this IHostApplicationBuilder builder)
    {
        if (!builder.Properties.TryAdd(HealthChecksAppliedKey, true))
        {
            return;
        }

        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    }
}
