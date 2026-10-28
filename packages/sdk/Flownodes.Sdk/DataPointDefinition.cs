using System.Text.Json;

namespace Flownodes.Sdk;

/// <summary>
/// The type of a Data Point's value.
/// </summary>
public enum DataPointType
{
    /// <summary>
    /// <c>true</c> or <c>false</c>.
    /// </summary>
    Bool,

    /// <summary>
    /// A whole number.
    /// </summary>
    Integer,

    /// <summary>
    /// Any number.
    /// </summary>
    Number,

    /// <summary>
    /// A text.
    /// </summary>
    String,

    /// <summary>
    /// One of a fixed set of texts.
    /// </summary>
    Enum
}

/// <summary>
/// A Data Point a Device exposes.
/// </summary>
/// <param name="Name">A flat DNS-label name, such as <c>temperature</c>.</param>
/// <param name="Type">The type of its value.</param>
/// <param name="Writable">Whether the Data Point can be written.</param>
/// <param name="Unit">The unit of a numeric value, such as <c>°C</c>.</param>
/// <param name="Min">The smallest value a numeric Data Point can hold.</param>
/// <param name="Max">The largest value a numeric Data Point can hold.</param>
/// <param name="Values">The allowed texts of an <see cref="DataPointType.Enum"/> Data Point.</param>
public sealed record DataPointDefinition(
    string Name,
    DataPointType Type,
    bool Writable = false,
    string? Unit = null,
    double? Min = null,
    double? Max = null,
    IReadOnlyList<string>? Values = null);

/// <summary>
/// An Action a Device or Connection offers.
/// </summary>
/// <param name="Name">A flat DNS-label name, such as <c>reboot</c>.</param>
/// <param name="SafeToRepeat">Whether calling it again with the same invocation id is harmless.</param>
/// <param name="Parameters">The JSON Schema of its parameters, if the Integration publishes one.</param>
/// <param name="Result">The JSON Schema of its result, if the Integration publishes one.</param>
public sealed record ActionDefinition(
    string Name,
    bool SafeToRepeat = false,
    JsonElement? Parameters = null,
    JsonElement? Result = null);

/// <summary>
/// An Event a Device or Connection can report, which can trigger a Flow.
/// </summary>
/// <param name="Name">A flat DNS-label name, such as <c>pressed</c>.</param>
/// <param name="Payload">The JSON Schema of its payload, if the Integration publishes one.</param>
public sealed record EventDefinition(string Name, JsonElement? Payload = null);

/// <summary>
/// What a Device offers, derived from its Spec by its Device Type (ADR-0010).
/// </summary>
/// <param name="DataPoints">Its Data Points.</param>
/// <param name="Actions">Its Actions.</param>
/// <param name="Events">Its Events.</param>
public sealed record DeviceSurface(
    IReadOnlyList<DataPointDefinition> DataPoints,
    IReadOnlyList<ActionDefinition> Actions,
    IReadOnlyList<EventDefinition> Events);

/// <summary>
/// What a Connection offers of its own: the Actions and Events of the External Services it reaches, which never have
/// Data Points (ADR-0011).
/// </summary>
/// <param name="Actions">Its Actions.</param>
/// <param name="Events">Its Events.</param>
public sealed record ConnectionSurface(IReadOnlyList<ActionDefinition> Actions, IReadOnlyList<EventDefinition> Events);
