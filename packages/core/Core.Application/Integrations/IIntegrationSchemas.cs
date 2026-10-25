using System.Text.Json;

namespace Core.Application.Integrations;

/// <summary>
/// One problem found in a configuration or an address.
/// </summary>
/// <param name="Path">The property the problem is about, relative to what was checked.</param>
/// <param name="Message">What is wrong.</param>
public sealed record SchemaProblem(string Path, string Message);

/// <summary>
/// What the Registry needs to know about a Device Type of an installed Integration to validate a Device (ADR-0010).
/// </summary>
/// <param name="Name">The name of the Device Type.</param>
/// <param name="ValidateAddress">Checks a Device's <c>address</c>.</param>
/// <param name="ValidateConfig">Checks a Device's <c>config</c>.</param>
public sealed record DeviceTypeSchema(
    string Name,
    Func<JsonElement, IReadOnlyList<SchemaProblem>> ValidateAddress,
    Func<JsonElement, IReadOnlyList<SchemaProblem>> ValidateConfig);

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
