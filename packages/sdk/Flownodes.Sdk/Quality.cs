namespace Flownodes.Sdk;

/// <summary>
/// The trustworthiness of a Data Point value.
/// </summary>
public enum Quality
{
    /// <summary>
    /// The value is current and trustworthy.
    /// </summary>
    Good,

    /// <summary>
    /// The value may be stale or approximate.
    /// </summary>
    Uncertain,

    /// <summary>
    /// The value cannot be trusted, for example because the Connection is lost.
    /// </summary>
    Bad
}
