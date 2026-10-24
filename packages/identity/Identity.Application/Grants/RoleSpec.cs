using Coderynx.Functional.Results;
using Identity.Domain.Users;

namespace Identity.Application.Grants;

internal sealed record RoleSpec(bool IsAdministrator, Role Role, string Namespace)
{
    public const string Administrator = "Administrator";

    public static Result<RoleSpec> Parse(string? role, string? @namespace)
    {
        if (string.Equals(role, Administrator, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(@namespace)
                ? Result.Found(new RoleSpec(true, default, string.Empty))
                : Errors.NamespaceNotAllowed();
        }

        if (!Enum.TryParse<Role>(role, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            return Errors.InvalidRole(role ?? string.Empty);
        }

        return string.IsNullOrEmpty(@namespace)
            ? Errors.NamespaceRequired(parsed.ToString())
            : Result.Found(new RoleSpec(false, parsed, @namespace));
    }
}
