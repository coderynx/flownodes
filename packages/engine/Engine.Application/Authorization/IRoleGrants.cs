using Coderynx.Functional.Results;
using Core.Domain.Authorization;

namespace Engine.Application.Authorization;

/// <summary>
/// The Engine's replica of Role Grants, which authorises every request (ADR-0026).
/// </summary>
public interface IRoleGrants
{
    /// <summary>
    /// Requires the caller to hold a specific role in a Namespace.
    /// </summary>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="role">The role the request needs.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>Success, or an error carrying <c>AccessErrors</c> codes.</returns>
    Task<Result> RequireRoleAsync(
        Guid callerId,
        string @namespace,
        NamespaceRole role,
        CancellationToken cancellationToken);

    /// <summary>
    /// Requires the caller to hold at least one of several roles in a Namespace.
    /// </summary>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="roles">The roles of which the request needs one.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>Success, or an error carrying <c>AccessErrors</c> codes.</returns>
    Task<Result> RequireAnyRoleAsync(
        Guid callerId,
        string @namespace,
        IReadOnlyCollection<NamespaceRole> roles,
        CancellationToken cancellationToken);

    /// <summary>
    /// Requires the caller to hold any role in a Namespace.
    /// </summary>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>Success, or an error carrying <c>AccessErrors</c> codes.</returns>
    Task<Result> RequireMemberAsync(Guid callerId, string @namespace, CancellationToken cancellationToken);

    /// <summary>
    /// Requires the caller to hold a role in at least one Namespace.
    /// </summary>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>Success, or an error carrying <c>AccessErrors</c> codes.</returns>
    Task<Result> RequireAnyMembershipAsync(Guid callerId, CancellationToken cancellationToken);
}
