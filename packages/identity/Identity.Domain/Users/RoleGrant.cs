namespace Identity.Domain.Users;

/// <summary>
/// A User holding the Operator or Engineer role in one Namespace.
/// </summary>
public sealed class RoleGrant
{
    private RoleGrant()
    {
        Namespace = string.Empty;
    }

    /// <summary>
    /// Gets the id of the User who holds the role.
    /// </summary>
    public Guid UserId { get; private set; }
    /// <summary>
    /// Gets the name of the Namespace the role applies to.
    /// </summary>
    public string Namespace { get; private set; }
    /// <summary>
    /// Gets the role held.
    /// </summary>
    public Role Role { get; private set; }

    internal static RoleGrant Create(Guid userId, string @namespace, Role role) =>
        new()
        {
            UserId = userId,
            Namespace = @namespace,
            Role = role
        };
}
