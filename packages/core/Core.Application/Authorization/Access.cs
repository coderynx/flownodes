using Coderynx.Functional.Results;
using Core.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Authorization;

/// <summary>
/// The authorisation checks a command handler runs first, against the module's own replica of Role Grants (ADR-0026).
/// </summary>
/// <remarks>
/// A caller who is unknown or disabled fails every check as unauthenticated. A caller with no role in a Namespace
/// gets a not-found error for anything in it, so its existence is not leaked; a member lacking the required role gets
/// a forbidden error. Administrator gives no access inside a Namespace.
/// </remarks>
public static class Access
{
    /// <summary>
    /// Requires the caller to be an Administrator.
    /// </summary>
    /// <param name="source">The module's database.</param>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The caller, or an error carrying <see cref="AccessErrors"/> codes.</returns>
    public static async Task<Result<UserAccess>> RequireAdministratorAsync(
        this IUserAccessSource source,
        Guid callerId,
        CancellationToken cancellationToken = default)
    {
        var caller = await FindActiveAsync(source, callerId, cancellationToken);

        return caller.IsSuccess && !caller.Value.IsAdministrator ? AccessErrors.NotAdministrator() : caller;
    }

    /// <summary>
    /// Requires the caller to hold a specific role in a Namespace.
    /// </summary>
    /// <param name="source">The module's database.</param>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="role">The role the request needs.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The caller, or an error carrying <see cref="AccessErrors"/> codes.</returns>
    public static async Task<Result<UserAccess>> RequireRoleAsync(
        this IUserAccessSource source,
        Guid callerId,
        string @namespace,
        NamespaceRole role,
        CancellationToken cancellationToken = default)
    {
        var caller = await RequireMemberAsync(source, callerId, @namespace, cancellationToken);

        return caller.IsSuccess && !caller.Value.Holds(@namespace, role)
            ? AccessErrors.MissingRole(@namespace, role.ToString())
            : caller;
    }

    /// <summary>
    /// Requires the caller to hold any role in a Namespace.
    /// </summary>
    /// <param name="source">The module's database.</param>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The caller, or an error carrying <see cref="AccessErrors"/> codes.</returns>
    public static async Task<Result<UserAccess>> RequireMemberAsync(
        this IUserAccessSource source,
        Guid callerId,
        string @namespace,
        CancellationToken cancellationToken = default)
    {
        var caller = await FindActiveAsync(source, callerId, cancellationToken);

        return caller.IsSuccess && !caller.Value.Holds(@namespace, null)
            ? AccessErrors.NamespaceNotFound(@namespace)
            : caller;
    }

    /// <summary>
    /// Resolves the Namespaces a caller may list: all of them for an Administrator, otherwise those where the caller
    /// holds a role.
    /// </summary>
    /// <param name="source">The module's database.</param>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The scope, or an unauthenticated error.</returns>
    public static async Task<Result<NamespaceScope>> ScopeAsync(
        this IUserAccessSource source,
        Guid callerId,
        CancellationToken cancellationToken = default)
    {
        var caller = await FindActiveAsync(source, callerId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var names = caller.Value.Grants.Select(grant => grant.Namespace).ToHashSet();

        return Result.Found(new NamespaceScope(caller.Value.IsAdministrator, names));
    }

    /// <summary>
    /// Decides a request from a replica that is not a database, such as the Engine's grains: the caller's state and the
    /// roles they hold in the Namespace.
    /// </summary>
    /// <param name="isActive">Whether the caller is a known, enabled User.</param>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="rolesHeld">The roles the caller holds in the Namespace.</param>
    /// <param name="required">The role the request needs; any role when <see langword="null"/>.</param>
    /// <returns>Success, or an error carrying <see cref="AccessErrors"/> codes.</returns>
    public static Result Authorise(
        bool isActive,
        string @namespace,
        IReadOnlyCollection<NamespaceRole> rolesHeld,
        NamespaceRole? required)
    {
        if (!isActive)
        {
            return AccessErrors.Unauthenticated();
        }

        if (rolesHeld.Count == 0)
        {
            return AccessErrors.NamespaceNotFound(@namespace);
        }

        return required is { } role && !rolesHeld.Contains(role)
            ? AccessErrors.MissingRole(@namespace, role.ToString())
            : Result.Updated();
    }

    /// <summary>
    /// Decides a request that belongs to no Namespace, which any member of some Namespace may make.
    /// </summary>
    /// <param name="isActive">Whether the caller is a known, enabled User.</param>
    /// <param name="roleCount">The number of Role Grants the caller holds.</param>
    /// <returns>Success, or an error carrying <see cref="AccessErrors"/> codes.</returns>
    public static Result AuthoriseMember(bool isActive, int roleCount)
    {
        if (!isActive)
        {
            return AccessErrors.Unauthenticated();
        }

        return roleCount == 0 ? AccessErrors.NoRole() : Result.Updated();
    }

    private static async Task<Result<UserAccess>> FindActiveAsync(
        IUserAccessSource source,
        Guid callerId,
        CancellationToken cancellationToken)
    {
        var caller = await source.UserAccess.AsNoTracking()
            .Include(user => user.Grants)
            .SingleOrDefaultAsync(user => user.Id == callerId, cancellationToken);

        return caller is { IsActive: true } ? Result.Found(caller) : AccessErrors.Unauthenticated();
    }
}
