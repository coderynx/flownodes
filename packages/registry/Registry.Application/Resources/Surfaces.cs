using System.Text.Json;
using Core.Application.Integrations;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal sealed class Surfaces(IIntegrationSchemas integrations)
{
    public ConnectionSurfaceSchema? Connection(NamespaceSnapshot @namespace, string connectionName)
    {
        if (@namespace.Find(ConnectionKind.KindName, connectionName) is not { } connection ||
            Integration(connection) is not { } integration)
        {
            return null;
        }

        var config = connection.TryGetProperty("config", out var given) ? given : ConnectionKind.EmptyObject;

        return integration.ValidateConfig(config).Count > 0 ? null : integration.Surface(config);
    }

    public DeviceSurfaceSchema? Device(NamespaceSnapshot @namespace, string deviceName)
    {
        if (@namespace.Find(DeviceKind.KindName, deviceName) is not { } device ||
            DeviceType(@namespace, device) is not { } deviceType)
        {
            return null;
        }

        if (!device.TryGetProperty("address", out var address))
        {
            return null;
        }

        var config = device.TryGetProperty("config", out var given) ? given : ConnectionKind.EmptyObject;
        if (deviceType.ValidateAddress(address).Count > 0 || deviceType.ValidateConfig(config).Count > 0)
        {
            return null;
        }

        return deviceType.Surface(address, config);
    }

    private IntegrationSchema? Integration(JsonElement connection) =>
        Text(connection, "integration") is { } integrationId ? integrations.Find(integrationId) : null;

    private DeviceTypeSchema? DeviceType(NamespaceSnapshot @namespace, JsonElement device)
    {
        if (Text(device, "connection") is not { } connectionName ||
            @namespace.Find(ConnectionKind.KindName, connectionName) is not { } connection)
        {
            return null;
        }

        var typeName = Text(device, "type");

        return Integration(connection)?.DeviceTypes.FirstOrDefault(candidate => candidate.Name == typeName);
    }

    private static string? Text(JsonElement spec, string name) =>
        spec.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
