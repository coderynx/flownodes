namespace Core.Contracts.Identity;

/// <summary>
/// Published by Identity when a module asks for a resync: the whole state of one User, deleted Users included
/// (ADR-0026). A receiver replaces its replica of the User with it, dropping every Role Grant the message does not
/// list.
/// </summary>
/// <param name="UserId">The immutable id of the User.</param>
/// <param name="Username">The User's username, or <see langword="null"/> for a deleted User.</param>
/// <param name="IsDisabled">Whether the User is disabled.</param>
/// <param name="IsDeleted">Whether Identity deleted the User.</param>
/// <param name="IsAdministrator">Whether the User holds the Administrator role.</param>
/// <param name="Grants">The Operator and Engineer roles the User holds, per Namespace.</param>
/// <param name="Seq">
/// The User's sequence number when it was published. A receiver drops a message whose number is not newer than the
/// last it applied for that User.
/// </param>
public sealed record UserAccessPublished(
    Guid UserId,
    string? Username,
    bool IsDisabled,
    bool IsDeleted,
    bool IsAdministrator,
    IReadOnlyList<PublishedRoleGrant> Grants,
    long Seq);
