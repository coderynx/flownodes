using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Core.Infrastructure.Bootstrap;

public static class DependencyInjection
{
    private const string OpenTelemetryAppliedKey = "Core.Infrastructure.OpenTelemetry.Applied";
    private const string HealthChecksAppliedKey = "Core.Infrastructure.HealthChecks.Applied";
    private const string HttpClientDefaultsAppliedKey = "Core.Infrastructure.HttpClientDefaults.Applied";

    /// <summary>
    /// Configures the HTTP client defaults: service discovery and the standard resilience handler.
    /// Safe to call once per module.
    /// </summary>
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
    /// Configures OpenTelemetry logging, metrics and tracing. Safe to call once per module.
    /// </summary>
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
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments(HostConfiguration.HealthEndpointPath)
                        && !context.Request.Path.StartsWithSegments(HostConfiguration.AlivenessEndpointPath))
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }

    /// <summary>
    /// Registers the liveness health check. Safe to call once per module.
    /// </summary>
    public static void AddHealthChecks(this IHostApplicationBuilder builder)
    {
        if (!builder.Properties.TryAdd(HealthChecksAppliedKey, true))
        {
            return;
        }

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    }
}
