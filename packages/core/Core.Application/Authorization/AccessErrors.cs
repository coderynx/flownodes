using Coderynx.Functional.Results.Errors;

namespace Core.Application.Authorization;

/// <summary>
/// The error codes of a refused authorisation check, so an endpoint can map them to HTTP statuses.
/// </summary>
public static class AccessErrors
{
    /// <summary>
    /// The caller is not an enabled User of the installation (HTTP 401).
    /// </summary>
    public const string UnauthenticatedCode = "access.unauthenticated";

    /// <summary>
    /// The caller is known, but lacks the role the request needs (HTTP 403).
    /// </summary>
    public const string ForbiddenCode = "access.forbidden";

    internal static Error Unauthenticated() =>
        Error.Custom(UnauthenticatedCode, "The caller is not an enabled User of this installation.");

    internal static Error NotAdministrator() =>
        Error.Custom(ForbiddenCode, "Only an Administrator can do this.");

    internal static Error MissingRole(string @namespace, string role) =>
        Error.Custom(ForbiddenCode, $"This needs the {role} role in Namespace '{@namespace}'.");

    internal static Error NoRole() =>
        Error.Custom(ForbiddenCode, "This needs a role in at least one Namespace.");

    internal static Error NamespaceNotFound(string @namespace) =>
        Error.NotFound("namespace.not_found", $"Namespace '{@namespace}' does not exist.");
}
