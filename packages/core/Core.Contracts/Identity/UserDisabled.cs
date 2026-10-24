namespace Core.Contracts.Identity;

/// <summary>
/// Published by Identity when a User is disabled. A disabled User holds no effective roles until enabled again
/// (ADR-0026).
/// </summary>
/// <param name="UserId">The immutable id of the User.</param>
/// <param name="Seq">
/// The User's gap-free sequence number for this message. A receiver drops a message whose number is not newer than the
/// last it applied for that User.
/// </param>
public sealed record UserDisabled(Guid UserId, long Seq);
