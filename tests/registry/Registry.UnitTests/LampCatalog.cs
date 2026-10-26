using System.Text.Json;
using Core.Application.Integrations;

namespace Registry.UnitTests;

/// <summary>
/// An Integration for tests only: <c>acme.lamp</c> has the Device Type <c>Bulb</c>, addressed by an integer id.
/// </summary>
internal sealed class LampCatalog : IIntegrationSchemas
{
    public IntegrationSchema? Find(string id) =>
        id == "acme.lamp"
            ? new IntegrationSchema(
                config => Check(config, "host", JsonValueKind.String),
                [
                    new DeviceTypeSchema(
                        "Bulb",
                        address => Check(address, "id", JsonValueKind.Number),
                        config => Check(config, null, JsonValueKind.Undefined),
                        (_, _) =>
                        [
                            new DeviceDataPointSchema("brightness", "number", true),
                            new DeviceDataPointSchema("level", "number", false),
                            new DeviceDataPointSchema("on", "bool", true)
                        ])
                ])
            : null;

    private static List<SchemaProblem> Check(JsonElement value, string? required, JsonValueKind kind)
    {
        var problems = value.EnumerateObject()
            .Where(property => property.Name != required)
            .Select(property => new SchemaProblem(property.Name, "Unknown field."))
            .ToList();
        if (required is null)
        {
            return problems;
        }

        if (!value.TryGetProperty(required, out var found))
        {
            problems.Add(new SchemaProblem(required, "Required."));
        }
        else if (found.ValueKind != kind)
        {
            problems.Add(new SchemaProblem(required, "Wrong type."));
        }

        return problems;
    }
}
