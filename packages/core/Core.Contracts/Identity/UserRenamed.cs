namespace Core.Contracts.Identity;

/// <summary>
/// Published by Identity when a User's username changes (ADR-0026).
/// </summary>
/// <param name="UserId">The immutable id of the User.</param>
/// <param name="Username">The new username, as written by whoever renamed the User.</param>
/// <param name="Seq">
/// The User's gap-free sequence number for this message. A receiver drops a message whose number is not newer than the
/// last it applied for that User.
/// </param>
public sealed record UserRenamed(Guid UserId, string Username, long Seq);
