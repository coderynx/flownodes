namespace Core.Domain.Authorization;

/// <summary>
/// A role a User holds in one Namespace.
/// </summary>
public enum NamespaceRole
{
    /// <summary>
    /// Watches live state, acknowledges Alarms and may invoke Actions, without changing configuration.
    /// </summary>
    Operator,

    /// <summary>
    /// Configures the Namespace's Connections, Secrets, Asset Types, Assets, Device attachments and Flows.
    /// </summary>
    Engineer
}
