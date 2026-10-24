using Coderynx.Functional.Results.Errors;
using Core.Application.Authorization;

namespace Identity.Application;

internal static class Errors
{
    public static Error Unauthenticated() =>
        Error.Custom(AccessErrors.UnauthenticatedCode, "The caller is not an enabled User of this installation.");

    public static Error Forbidden() =>
        Error.Custom(AccessErrors.ForbiddenCode, "Only an Administrator can do this.");

    public static Error ForbiddenOtherUser() =>
        Error.Custom(AccessErrors.ForbiddenCode, "A User can manage only their own Personal Access Tokens.");

    public static Error InvalidUsername(string username) =>
        Error.InvalidInput(
            "user.invalid_username",
            $"'{username}' is not a valid username. Use 1 to 64 letters, digits, dots, underscores, hyphens or at " +
            "signs.");

    public static Error UsernameTaken(string username) =>
        Error.Conflict("user.username_taken", $"The username '{username}' is already taken.");

    public static Error InvalidPassword(IEnumerable<string> reasons) =>
        Error.InvalidInput("user.invalid_password", string.Join(' ', reasons));

    public static Error UserNotFound() => Error.NotFound("user.not_found", "The User does not exist.");

    public static Error LastAdministrator() =>
        Error.Conflict("user.last_administrator", "The installation must keep at least one active Administrator.");

    public static Error InvalidCredentials() =>
        Error.InvalidInput("signin.invalid_credentials", "The username or password is wrong.");

    public static Error LockedOut() =>
        Error.InvalidInput("signin.locked_out", "Too many failed attempts. Try again in a few minutes.");

    public static Error WrongCurrentPassword() =>
        Error.InvalidInput("user.wrong_current_password", "The current password is wrong.");

    public static Error SetupDone() =>
        Error.NotFound("setup.done", "The installation already has a User, so there is nothing to set up.");

    public static Error InvalidSetupToken() =>
        Error.InvalidInput("setup.invalid_token", "The setup token is wrong. Read it from the Silo's log.");

    public static Error InvalidRole(string role) =>
        Error.InvalidInput(
            "role_grant.invalid_role",
            $"'{role}' is not a role. Use Administrator, Engineer or Operator.");

    public static Error NamespaceRequired(string role) =>
        Error.InvalidInput("role_grant.namespace_required", $"The {role} role is granted in a Namespace.");

    public static Error NamespaceNotAllowed() =>
        Error.InvalidInput(
            "role_grant.namespace_not_allowed",
            "The Administrator role is installation-wide and takes no Namespace.");

    public static Error UnknownNamespace(string name) =>
        Error.InvalidInput("role_grant.unknown_namespace", $"Namespace '{name}' does not exist.");

    public static Error RoleNotHeld() =>
        Error.NotFound("role_grant.not_held", "The User does not hold that role.");

    public static Error InvalidTokenName() =>
        Error.InvalidInput("token.invalid_name", "A token needs a name of 1 to 100 characters.");

    public static Error ExpiryInThePast() =>
        Error.InvalidInput("token.expiry_in_the_past", "A token cannot expire in the past.");

    public static Error TokenNotFound() => Error.NotFound("token.not_found", "The token does not exist.");

    public static Error InvalidToken() =>
        Error.InvalidInput("token.invalid", "The token is unknown, revoked or expired.");

    public static Error UnknownSortKey(string key) =>
        Error.InvalidInput("user.unknown_sort_key", $"Cannot sort Users by '{key}'. Use: username.");
}
