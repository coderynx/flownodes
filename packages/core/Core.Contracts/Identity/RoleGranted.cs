namespace Core.Contracts.Identity;

/// <summary>
/// Published by Identity when a User is granted the Operator or Engineer role in a Namespace (ADR-0026).
/// </summary>
/// <param name="UserId">The immutable id of the User.</param>
/// <param name="Namespace">The name of the Namespace the role applies to.</param>
/// <param name="Role">The role granted: <c>Operator</c> or <c>Engineer</c>.</param>
/// <param name="Seq">
/// The User's gap-free sequence number for this message. A receiver drops a message whose number is not newer than the
/// last it applied for that User.
/// </param>
public sealed record RoleGranted(Guid UserId, string Namespace, string Role, long Seq);
