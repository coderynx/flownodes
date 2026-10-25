using System.Text.Json;
using Core.Application.Integrations;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

/// <summary>
/// The Connection Kind: a configured instance of an installed Integration. Its <c>config</c> is checked against the
/// schema the Integration publishes (ADR-0009).
/// </summary>
internal sealed class ConnectionKind(IIntegrationSchemas integrations) : IResourceKind
{
    public const string KindName = "Connection";
    public string Name => KindName;
    public string Plural => "connections";

    public IEnumerable<SpecError> Validate(JsonElement spec, NamespaceSnapshot @namespace)
    {
        foreach (var field in spec.EnumerateObject().Where(field => field.Name is not ("integration" or "config")))
        {
            yield return new SpecError($"spec.{field.Name}", "Unknown field.");
        }

        if (!spec.TryGetProperty("integration", out var integration) || integration.ValueKind != JsonValueKind.String)
        {
            yield return new SpecError("spec.integration", "Required, as the id of an installed Integration.");

            yield break;
        }

        if (integrations.Find(integration.GetString()!) is not { } schema)
        {
            yield return new SpecError(
                "spec.integration",
                $"Integration '{integration.GetString()}' is not installed.");

            yield break;
        }

        var config = spec.TryGetProperty("config", out var given) ? given : EmptyObject;
        foreach (var problem in schema.ValidateConfig(config))
        {
            yield return new SpecError(Path("spec.config", problem.Path), problem.Message);
        }
    }

    public IEnumerable<SpecReference> References(JsonElement spec) => [];
    internal static JsonElement EmptyObject { get; } = JsonDocument.Parse("{}").RootElement;

    internal static string Path(string root, string relative) =>
        relative.Length == 0 ? root : $"{root}.{relative}";
}
