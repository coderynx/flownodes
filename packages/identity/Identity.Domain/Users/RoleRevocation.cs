namespace Identity.Domain.Users;

/// <summary>
/// One Role Grant taken away from a User, with the sequence number its message carries.
/// </summary>
/// <param name="Role">The role that was held.</param>
/// <param name="Seq">The User's sequence number for the revocation.</param>
public sealed record RoleRevocation(Role Role, long Seq);
