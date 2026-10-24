namespace Identity.Domain.Users;

/// <summary>
/// A User holding the Operator or Engineer role in one Namespace.
/// </summary>
public sealed class RoleGrant
{
    private RoleGrant()
    {
    }

    /// <summary>
    /// Gets the id of the User who holds the role.
    /// </summary>
    public required Guid UserId { get; init; }
    /// <summary>
    /// Gets the name of the Namespace the role applies to.
    /// </summary>
    public required string Namespace { get; init; }
    /// <summary>
    /// Gets the role held.
    /// </summary>
    public required Role Role { get; init; }

    internal static RoleGrant Create(Guid userId, string @namespace, Role role) =>
        new()
        {
            UserId = userId,
            Namespace = @namespace,
            Role = role
        };
}
