using System.Reflection;
using Coderynx.MediatorKit.Abstractions;
using JasperFx.CodeGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence.Durability;
using Wolverine.Postgresql;

namespace Core.Infrastructure.Bootstrap;

/// <summary>
/// Registers the infrastructure every module shares: telemetry, health checks and outgoing HTTP defaults.
/// </summary>
/// <remarks>
/// Every module's Infrastructure calls each of these, so each one applies once per host and ignores later calls.
/// </remarks>
public static class DependencyInjection
{
    private const string MessagingOptionsKey = "Core.Infrastructure.Messaging.Options";
    private const string OpenTelemetryAppliedKey = "Core.Infrastructure.OpenTelemetry.Applied";
    private const string HealthChecksAppliedKey = "Core.Infrastructure.HealthChecks.Applied";
    private const string HttpClientDefaultsAppliedKey = "Core.Infrastructure.HttpClientDefaults.Applied";

    /// <summary>
    /// Adds a module's database to the host's one Wolverine, the messaging between modules (ADR-0013).
    /// </summary>
    /// <remarks>
    /// The first module to call this makes its database the Wolverine message store and the home of the Postgres
    /// queues. Every later module keeps its own outbox in its own database, as an ancillary store, and adds its
    /// routing to the same Wolverine.
    /// </remarks>
    /// <param name="builder">The host the module is being added to.</param>
    /// <param name="connectionString">The module's Postgres connection string.</param>
    /// <param name="configure">Adds the module's publishing rules and queues.</param>
    /// <typeparam name="TDbContext">The module's database context, which writes the outbox.</typeparam>
    public static void AddMessaging<TDbContext>(
        this IHostApplicationBuilder builder,
        string connectionString,
        Action<WolverineOptions> configure) where TDbContext : DbContext
    {
        if (builder.Properties.TryGetValue(MessagingOptionsKey, out var existing))
        {
            var shared = (WolverineOptions)existing;
            shared.PersistMessagesWithPostgresql(connectionString, role: MessageStoreRole.Ancillary)
                .Enroll<TDbContext>();

            configure(shared);

            return;
        }

        builder.Services.AddWolverine(options =>
        {
            builder.Properties[MessagingOptionsKey] = options;

            options.ServiceName = "flownodes";
            options.ApplicationAssembly = Assembly.GetEntryAssembly();
            options.Durability.Mode = DurabilityMode.Solo;
            options.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
            options.CodeGeneration.AlwaysUseServiceLocationFor<ISender>();

            options.UsePostgresqlPersistenceAndTransport(connectionString).EnableQueueNotifications();
            options.UseEntityFrameworkCoreTransactions();

            configure(options);
        });
    }

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
