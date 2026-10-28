using System.Text.Json;

namespace Core.Application.Integrations;

/// <summary>
/// One problem found in a configuration or an address.
/// </summary>
/// <param name="Path">The property the problem is about, relative to what was checked.</param>
/// <param name="Message">What is wrong.</param>
public sealed record SchemaProblem(string Path, string Message);

/// <summary>
/// One Data Point of a Device, as far as the Registry needs it to validate a reference to it (ADR-0010).
/// </summary>
/// <param name="Name">The Data Point's name.</param>
/// <param name="Type">
/// The type of its value: <c>bool</c>, <c>integer</c>, <c>number</c>, <c>string</c> or <c>enum</c>.
/// </param>
/// <param name="Writable">Whether it can be written.</param>
public sealed record DeviceDataPointSchema(string Name, string Type, bool Writable);

/// <summary>
/// What a Device offers, as far as the Registry needs it to validate a reference to it (ADR-0010).
/// </summary>
/// <param name="DataPoints">Its Data Points.</param>
/// <param name="Actions">The names of its Actions.</param>
/// <param name="Events">The names of its Events.</param>
public sealed record DeviceSurfaceSchema(
    IReadOnlyList<DeviceDataPointSchema> DataPoints,
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> Events);

/// <summary>
/// What the Registry needs to know about a Device Type of an installed Integration to validate a Device (ADR-0010).
/// </summary>
/// <param name="Name">The name of the Device Type.</param>
/// <param name="ValidateAddress">Checks a Device's <c>address</c>.</param>
/// <param name="ValidateConfig">Checks a Device's <c>config</c>.</param>
/// <param name="Surface">Derives what a Device offers from its <c>address</c> and <c>config</c>.</param>
public sealed record DeviceTypeSchema(
    string Name,
    Func<JsonElement, IReadOnlyList<SchemaProblem>> ValidateAddress,
    Func<JsonElement, IReadOnlyList<SchemaProblem>> ValidateConfig,
    Func<JsonElement, JsonElement, DeviceSurfaceSchema> Surface);

/// <summary>
/// What the Registry needs to know about an installed Integration to validate a Connection (ADR-0009).
/// </summary>
/// <param name="ValidateConfig">Checks a Connection's <c>config</c>.</param>
/// <param name="DeviceTypes">The Device Types the Integration defines.</param>
public sealed record IntegrationSchema(
    Func<JsonElement, IReadOnlyList<SchemaProblem>> ValidateConfig,
    IReadOnlyList<DeviceTypeSchema> DeviceTypes);

/// <summary>
/// The schemas of the Integrations installed on this Silo, which the Registry validates Specs against (ADR-0012).
/// </summary>
public interface IIntegrationSchemas
{
    /// <summary>
    /// Finds the schema of an installed Integration.
    /// </summary>
    /// <param name="id">The vendor-qualified id.</param>
    /// <returns>The schema, or <see langword="null"/> when no Integration with that id loaded.</returns>
    IntegrationSchema? Find(string id);
}
