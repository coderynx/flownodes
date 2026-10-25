using System.Text.Json;
using Core.Application.Integrations;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

/// <summary>
/// The Device Kind: a Device declared on a Connection at an Integration-defined address (ADR-0008). Its <c>address</c>
/// and <c>config</c> are checked against its Device Type (ADR-0010), and its optional <c>asset</c> attaches it to at
/// most one Asset (ADR-0008).
/// </summary>
internal sealed class DeviceKind(IIntegrationSchemas integrations) : IResourceKind
{
    public const string KindName = "Device";
    public string Name => KindName;
    public string Plural => "devices";

    public IEnumerable<SpecError> Validate(JsonElement spec, NamespaceSnapshot @namespace)
    {
        foreach (var field in spec.EnumerateObject()
                     .Where(field => field.Name is not ("connection" or "type" or "address" or "config" or "asset")))
        {
            yield return new SpecError($"spec.{field.Name}", "Unknown field.");
        }

        var connectionName = Text(spec, "connection");
        var typeName = Text(spec, "type");
        if (connectionName is null)
        {
            yield return new SpecError("spec.connection", "Required, as the name of a Connection.");
        }

        if (typeName is null)
        {
            yield return new SpecError("spec.type", "Required, as the name of a Device Type of the Integration.");
        }

        if (!spec.TryGetProperty("address", out var address))
        {
            yield return new SpecError("spec.address", "Required.");
        }

        if (spec.TryGetProperty("asset", out var asset) && asset.ValueKind != JsonValueKind.String)
        {
            yield return new SpecError("spec.asset", "Must be the name of an Asset.");
        }

        var connection = connectionName is null ? null : @namespace.Find(ConnectionKind.KindName, connectionName);
        if (connection is not { } declared || typeName is null)
        {
            yield break;
        }

        if (address.ValueKind == JsonValueKind.Undefined)
        {
            yield break;
        }

        var integrationId = Text(declared, "integration");
        var schema = integrationId is null ? null : integrations.Find(integrationId);
        if (schema is null)
        {
            yield return new SpecError(
                "spec.connection",
                $"Connection '{connectionName}' uses an Integration that is not installed.");

            yield break;
        }

        var deviceType = schema.DeviceTypes.FirstOrDefault(candidate => candidate.Name == typeName);
        if (deviceType is null)
        {
            yield return new SpecError("spec.type", $"Integration '{integrationId}' has no Device Type '{typeName}'.");

            yield break;
        }

        foreach (var problem in deviceType.ValidateAddress(address))
        {
            yield return new SpecError(ConnectionKind.Path("spec.address", problem.Path), problem.Message);
        }

        var config = spec.TryGetProperty("config", out var given) ? given : ConnectionKind.EmptyObject;
        foreach (var problem in deviceType.ValidateConfig(config))
        {
            yield return new SpecError(ConnectionKind.Path("spec.config", problem.Path), problem.Message);
        }
    }

    public IEnumerable<SpecReference> References(JsonElement spec)
    {
        if (Text(spec, "connection") is { } connection)
        {
            yield return new SpecReference(ConnectionKind.KindName, connection, "spec.connection");
        }

        if (Text(spec, "asset") is { } asset)
        {
            yield return new SpecReference(AssetKind.KindName, asset, "spec.asset");
        }
    }

    private static string? Text(JsonElement spec, string name) =>
        spec.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
