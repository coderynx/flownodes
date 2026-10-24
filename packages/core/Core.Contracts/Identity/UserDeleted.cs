namespace Core.Contracts.Identity;

/// <summary>
/// Published by Identity when a User is deleted. It is the last message about that User (ADR-0026).
/// </summary>
/// <param name="UserId">The immutable id of the User.</param>
/// <param name="Seq">
/// The User's gap-free sequence number for this message. A receiver drops a message whose number is not newer than the
/// last it applied for that User.
/// </param>
public sealed record UserDeleted(Guid UserId, long Seq);
