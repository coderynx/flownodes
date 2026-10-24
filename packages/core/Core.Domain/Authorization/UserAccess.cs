namespace Core.Domain.Authorization;

/// <summary>
/// A module's replica of one User and their Role Grants, built from Identity's messages (ADR-0026). A module
/// authorises every request from its own replica, never by asking Identity.
/// </summary>
/// <remarks>
/// Identity numbers its messages per User without gaps. The replica applies a message only when its number is newer
/// than the last it applied, so a duplicated or reordered message changes nothing.
/// </remarks>
public sealed class UserAccess
{
    private readonly List<NamespaceGrant> _grants = [];

    private UserAccess()
    {
    }

    /// <summary>
    /// Gets the immutable id of the User, the subject of their access tokens.
    /// </summary>
    public Guid Id { get; private set; }
    /// <summary>
    /// Gets the User's current username, or <see langword="null"/> before Identity has reported it.
    /// </summary>
    public string? Username { get; private set; }
    /// <summary>
    /// Gets a value indicating whether the User is disabled.
    /// </summary>
    public bool IsDisabled { get; private set; }
    /// <summary>
    /// Gets a value indicating whether Identity deleted the User. The replica keeps the username for history.
    /// </summary>
    public bool IsDeleted { get; private set; }
    /// <summary>
    /// Gets a value indicating whether the User holds the Administrator role.
    /// </summary>
    public bool IsAdministrator { get; private set; }
    /// <summary>
    /// Gets the number of the last message applied for this User.
    /// </summary>
    public long LastSeq { get; private set; }
    /// <summary>
    /// Gets the Operator and Engineer roles the User holds, whether or not they are currently effective.
    /// </summary>
    public IReadOnlyList<NamespaceGrant> Grants => _grants;
    /// <summary>
    /// Gets a value indicating whether the User may call at all: neither disabled nor deleted.
    /// </summary>
    public bool IsActive => !IsDisabled && !IsDeleted;

    /// <summary>
    /// Starts the replica of a User Identity has not reported before.
    /// </summary>
    /// <param name="id">The immutable id of the User.</param>
    /// <returns>The new replica, holding no roles.</returns>
    public static UserAccess Create(Guid id) =>
        new()
        {
            Id = id
        };

    /// <summary>
    /// Tells whether the User holds a role in a Namespace.
    /// </summary>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="role">The role to look for; any role when <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the User is active and holds the role.</returns>
    public bool Holds(string @namespace, NamespaceRole? role) =>
        IsActive && _grants.Any(grant => grant.Namespace == @namespace && (role is null || grant.Role == role));

    /// <summary>
    /// Records a new username.
    /// </summary>
    /// <param name="username">The new username.</param>
    /// <param name="seq">The number of Identity's message.</param>
    public void Rename(string username, long seq)
    {
        if (Advance(seq))
        {
            Username = username;
        }
    }

    /// <summary>
    /// Records that the User is disabled.
    /// </summary>
    /// <param name="seq">The number of Identity's message.</param>
    public void Disable(long seq)
    {
        if (Advance(seq))
        {
            IsDisabled = true;
        }
    }

    /// <summary>
    /// Records that the User is enabled again.
    /// </summary>
    /// <param name="seq">The number of Identity's message.</param>
    public void Enable(long seq)
    {
        if (Advance(seq))
        {
            IsDisabled = false;
        }
    }

    /// <summary>
    /// Records that the User was deleted, which drops every role.
    /// </summary>
    /// <param name="seq">The number of Identity's message.</param>
    public void Delete(long seq)
    {
        if (!Advance(seq))
        {
            return;
        }

        IsDeleted = true;
        IsAdministrator = false;
        _grants.Clear();
    }

    /// <summary>
    /// Records that the User became an Administrator.
    /// </summary>
    /// <param name="seq">The number of Identity's message.</param>
    public void GrantAdministrator(long seq)
    {
        if (Advance(seq))
        {
            IsAdministrator = true;
        }
    }

    /// <summary>
    /// Records that the User is no longer an Administrator.
    /// </summary>
    /// <param name="seq">The number of Identity's message.</param>
    public void RevokeAdministrator(long seq)
    {
        if (Advance(seq))
        {
            IsAdministrator = false;
        }
    }

    /// <summary>
    /// Records a role the User was granted in a Namespace.
    /// </summary>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="role">The role granted.</param>
    /// <param name="seq">The number of Identity's message.</param>
    public void GrantRole(string @namespace, NamespaceRole role, long seq)
    {
        if (Advance(seq) && !_grants.Any(grant => grant.Namespace == @namespace && grant.Role == role))
        {
            _grants.Add(NamespaceGrant.Create(@namespace, role));
        }
    }

    /// <summary>
    /// Records a role the User lost in a Namespace.
    /// </summary>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="role">The role revoked.</param>
    /// <param name="seq">The number of Identity's message.</param>
    public void RevokeRole(string @namespace, NamespaceRole role, long seq)
    {
        if (Advance(seq))
        {
            _grants.RemoveAll(grant => grant.Namespace == @namespace && grant.Role == role);
        }
    }

    private bool Advance(long seq)
    {
        if (seq <= LastSeq)
        {
            return false;
        }

        LastSeq = seq;

        return true;
    }
}
