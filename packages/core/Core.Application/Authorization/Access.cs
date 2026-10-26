using Coderynx.Functional.Results;
using Core.Domain.Authorization;

namespace Core.Application.Authorization;

/// <summary>
/// The authorisation decisions that do not need a database, for a module whose replica of Role Grants is not one
/// (ADR-0026). <see cref="IAccessChecks"/> runs the same rules against a database.
/// </summary>
/// <remarks>
/// A caller who is unknown or disabled fails every check as unauthenticated. A caller with no role in a Namespace
/// gets a not-found error for anything in it, so its existence is not leaked; a member lacking the required role gets
/// a forbidden error. Administrator gives no access inside a Namespace.
/// </remarks>
public static class Access
{
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
    /// Decides a request from a replica that is not a database, when any one of several roles is enough.
    /// </summary>
    /// <param name="isActive">Whether the caller is a known, enabled User.</param>
    /// <param name="namespace">The name of the Namespace.</param>
    /// <param name="rolesHeld">The roles the caller holds in the Namespace.</param>
    /// <param name="anyOf">The roles of which the request needs one.</param>
    /// <returns>Success, or an error carrying <see cref="AccessErrors"/> codes.</returns>
    public static Result AuthoriseAnyOf(
        bool isActive,
        string @namespace,
        IReadOnlyCollection<NamespaceRole> rolesHeld,
        IReadOnlyCollection<NamespaceRole> anyOf)
    {
        var member = Authorise(isActive, @namespace, rolesHeld, null);

        return member.IsSuccess && !rolesHeld.Intersect(anyOf).Any()
            ? AccessErrors.MissingRole(@namespace, string.Join(" or ", anyOf))
            : member;
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
}
