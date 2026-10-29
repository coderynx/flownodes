namespace Core.Contracts.Identity;

/// <summary>
/// One Role Grant in a <see cref="UserAccessPublished"/> message.
/// </summary>
/// <param name="Namespace">The name of the Namespace the role applies to.</param>
/// <param name="Role">The role held: <c>Operator</c> or <c>Engineer</c>.</param>
public sealed record PublishedRoleGrant(string Namespace, string Role);
