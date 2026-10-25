using System.Text.Json;

namespace Flownodes.Sdk;

/// <summary>
/// A kind of Device an Integration defines.
/// </summary>
/// <param name="Name">The name Engineers see, such as <c>Thermometer</c>.</param>
/// <param name="Address">The shape of the address that finds a Device of this type on its Connection.</param>
/// <param name="Config">The shape of the optional configuration of a Device of this type.</param>
/// <param name="Surface">
/// Derives the Data Points and Actions of a Device from its address and configuration. It runs while a Manifest is
/// applied, so it must be synchronous, deterministic and free of I/O (ADR-0010).
/// </param>
public sealed record DeviceType(
    string Name,
    ConfigSchema Address,
    ConfigSchema Config,
    Func<JsonElement, JsonElement, DeviceSurface> Surface);
