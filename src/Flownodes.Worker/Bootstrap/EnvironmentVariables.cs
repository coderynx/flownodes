using Flownodes.Worker.Extensions;

namespace Flownodes.Worker.Bootstrap;

internal static class EnvironmentVariables
{
    public static string? RedisConnectionString => Environment.GetEnvironmentVariable("REDIS");
    public static string? OrleansServiceId => Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID");
    public static string? OrleansClusterId => Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID");
    public static int? OrleansGatewayPort => Environment.GetEnvironmentVariable("ORLEANS_GATEWAY_PORT").ToNullableInt();
    public static int? OrleansSiloPort => Environment.GetEnvironmentVariable("ORLEANS_SILO_PORT").ToNullableInt();
    public static string? AdminSecret => Environment.GetEnvironmentVariable("ADMIN_SECRET");
}