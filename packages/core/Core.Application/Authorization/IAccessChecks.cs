using Coderynx.Functional.Results;
using Core.Domain.Authorization;

namespace Core.Application.Authorization;

/// <summary>
/// The authorisation checks a command handler runs first, against the module's own replica of Role Grants (ADR-0026).
/// </summary>
/// <remarks>
/// A caller who is unknown or disabled fails every check as unauthenticated. A caller with no role in a Namespace
/// gets a not-found error for anything in it, so its existence is not leaked; a member lacking the required role gets
/// a forbidden error. Administrator gives no access inside a Namespace.
/// </remarks>
public interface IAccessChecks
{
    /// <summary>
    /// Requires the caller to be an Administrator.
    /// </summary>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The caller, or an error carrying <see cref="AccessErrors"/> codes.</returns>
    Task<Result<UserAccess>> RequireAdministratorAsync(Guid callerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requires the caller to hold a specific role in a Namespace.
    /// </summary>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="role">The role the request needs.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The caller, or an error carrying <see cref="AccessErrors"/> codes.</returns>
    Task<Result<UserAccess>> RequireRoleAsync(
        Guid callerId,
        string @namespace,
        NamespaceRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requires the caller to hold any role in a Namespace.
    /// </summary>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The caller, or an error carrying <see cref="AccessErrors"/> codes.</returns>
    Task<Result<UserAccess>> RequireMemberAsync(
        Guid callerId,
        string @namespace,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the Namespaces a caller may list: all of them for an Administrator, otherwise those where the caller
    /// holds a role.
    /// </summary>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The scope, or an unauthenticated error.</returns>
    Task<Result<NamespaceScope>> ScopeAsync(Guid callerId, CancellationToken cancellationToken = default);
}
