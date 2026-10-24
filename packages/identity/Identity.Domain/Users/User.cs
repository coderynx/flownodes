namespace Identity.Domain.Users;

/// <summary>
/// A person who can sign in to the installation, with their credentials and Role Grants. Every change other modules
/// must learn about advances the User's own sequence number.
/// </summary>
public sealed class User
{
    private readonly List<RoleGrant> _roleGrants = [];

    private User()
    {
    }

    /// <summary>
    /// Gets the immutable id of the User, which access tokens carry as their subject.
    /// </summary>
    public required Guid Id { get; init; }
    /// <summary>
    /// Gets the name the User signs in with.
    /// </summary>
    public Username Username { get; private set; } = null!;
    /// <summary>
    /// Gets the optional name shown instead of the username.
    /// </summary>
    public string? DisplayName { get; private set; }
    /// <summary>
    /// Gets the hash of the User's password; <see langword="null"/> until one is set.
    /// </summary>
    public string? PasswordHash { get; private set; }
    /// <summary>
    /// Gets how many sign-in attempts with a wrong password failed since the last success or lockout.
    /// </summary>
    public int AccessFailedCount { get; private set; }
    /// <summary>
    /// Gets the moment the User's lockout ends; <see langword="null"/> when the User was never locked out.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; private set; }
    /// <summary>
    /// Gets a value indicating whether the User is disabled: unable to sign in or use tokens.
    /// </summary>
    public bool IsDisabled { get; private set; }
    /// <summary>
    /// Gets a value indicating whether the User holds the installation-wide Administrator role.
    /// </summary>
    public bool IsAdministrator { get; private set; }
    /// <summary>
    /// Gets the sequence number of the last message published about the User.
    /// </summary>
    public long Seq { get; private set; }
    /// <summary>
    /// Gets the Operator and Engineer roles the User holds, per Namespace.
    /// </summary>
    public IReadOnlyCollection<RoleGrant> RoleGrants => _roleGrants;

    /// <summary>
    /// Creates a User with no password, no roles and sequence number 0.
    /// </summary>
    /// <param name="username">The name the User signs in with.</param>
    /// <param name="displayName">The optional name shown instead of the username.</param>
    /// <returns>The new User.</returns>
    public static User Create(Username username, string? displayName) =>
        new()
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim()
        };

    /// <summary>
    /// Changes the username, advancing the sequence number when it differs.
    /// </summary>
    /// <param name="username">The new username.</param>
    /// <returns><see langword="true"/> when the username changed.</returns>
    public bool Rename(Username username)
    {
        if (Username.Value == username.Value)
        {
            return false;
        }

        Username = username;
        Seq++;

        return true;
    }

    /// <summary>
    /// Changes the display name; no message is published about it.
    /// </summary>
    /// <param name="displayName">The new display name, or <see langword="null"/> to show the username.</param>
    public void ChangeDisplayName(string? displayName)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }

    /// <summary>
    /// Replaces the stored password hash.
    /// </summary>
    /// <param name="passwordHash">The hash of the new password.</param>
    public void SetPasswordHash(string? passwordHash)
    {
        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Counts a failed sign-in attempt.
    /// </summary>
    /// <returns>How many attempts have failed since the last success or lockout.</returns>
    public int RecordFailedAccess() => ++AccessFailedCount;

    /// <summary>
    /// Forgets the failed sign-in attempts.
    /// </summary>
    public void ResetFailedAccess()
    {
        AccessFailedCount = 0;
    }

    /// <summary>
    /// Locks the User out of signing in until the given moment.
    /// </summary>
    /// <param name="end">The moment the lockout ends.</param>
    public void LockOutUntil(DateTimeOffset? end)
    {
        LockoutEnd = end;
    }

    /// <summary>
    /// Disables the User, advancing the sequence number when the User was enabled.
    /// </summary>
    /// <returns><see langword="true"/> when the User was enabled before.</returns>
    public bool Disable()
    {
        if (IsDisabled)
        {
            return false;
        }

        IsDisabled = true;
        Seq++;

        return true;
    }

    /// <summary>
    /// Enables the User, advancing the sequence number when the User was disabled.
    /// </summary>
    /// <returns><see langword="true"/> when the User was disabled before.</returns>
    public bool Enable()
    {
        if (!IsDisabled)
        {
            return false;
        }

        IsDisabled = false;
        Seq++;

        return true;
    }

    /// <summary>
    /// Makes the User an Administrator, advancing the sequence number when they were not one.
    /// </summary>
    /// <returns><see langword="true"/> when the User was not an Administrator before.</returns>
    public bool GrantAdministrator()
    {
        if (IsAdministrator)
        {
            return false;
        }

        IsAdministrator = true;
        Seq++;

        return true;
    }

    /// <summary>
    /// Stops the User being an Administrator, advancing the sequence number when they were one.
    /// </summary>
    /// <returns><see langword="true"/> when the User was an Administrator before.</returns>
    public bool RevokeAdministrator()
    {
        if (!IsAdministrator)
        {
            return false;
        }

        IsAdministrator = false;
        Seq++;

        return true;
    }

    /// <summary>
    /// Grants a role in a Namespace, advancing the sequence number when the User did not hold it.
    /// </summary>
    /// <param name="namespace">The name of the Namespace the role applies to.</param>
    /// <param name="role">The role to grant.</param>
    /// <returns><see langword="true"/> when the User did not hold the role before.</returns>
    public bool Grant(string @namespace, Role role)
    {
        if (_roleGrants.Exists(grant => grant.Namespace == @namespace && grant.Role == role))
        {
            return false;
        }

        _roleGrants.Add(RoleGrant.Create(Id, @namespace, role));
        Seq++;

        return true;
    }

    /// <summary>
    /// Revokes a role in a Namespace, advancing the sequence number when the User held it.
    /// </summary>
    /// <param name="namespace">The name of the Namespace the role applied to.</param>
    /// <param name="role">The role to revoke.</param>
    /// <returns><see langword="true"/> when the User held the role before.</returns>
    public bool Revoke(string @namespace, Role role)
    {
        var held = _roleGrants.Find(grant => grant.Namespace == @namespace && grant.Role == role);
        if (held is null)
        {
            return false;
        }

        _roleGrants.Remove(held);
        Seq++;

        return true;
    }

    /// <summary>
    /// Revokes every role the User holds in a Namespace, advancing the sequence number once per role.
    /// </summary>
    /// <param name="namespace">The name of the Namespace that was deleted.</param>
    /// <returns>The roles that were revoked, each with its sequence number.</returns>
    public IReadOnlyList<RoleRevocation> RevokeAllIn(string @namespace)
    {
        var revoked = new List<RoleRevocation>();

        foreach (var grant in _roleGrants.Where(grant => grant.Namespace == @namespace).ToList())
        {
            _roleGrants.Remove(grant);
            revoked.Add(new RoleRevocation(grant.Role, ++Seq));
        }

        return revoked;
    }

    /// <summary>
    /// Advances the sequence number to the one the deletion message carries.
    /// </summary>
    public void Delete()
    {
        Seq++;
    }
}
