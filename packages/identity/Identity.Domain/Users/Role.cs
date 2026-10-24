namespace Identity.Domain.Users;

/// <summary>
/// A role a User holds within one Namespace. The installation-wide Administrator role is not one of them.
/// </summary>
public enum Role
{
    /// <summary>
    /// Watches live state, acknowledges Alarms and may invoke Actions, without changing configuration.
    /// </summary>
    Operator = 1,

    /// <summary>
    /// Configures a Namespace's Connections, Secrets, Asset Types, Assets, Device attachments and Flows.
    /// </summary>
    Engineer = 2
}
