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

        if (role is null || !Enum.GetNames<Role>().Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return Errors.InvalidRole(role ?? string.Empty);
        }

        var parsed = Enum.Parse<Role>(role, true);

        return string.IsNullOrEmpty(@namespace)
            ? Errors.NamespaceRequired(parsed.ToString())
            : Result.Found(new RoleSpec(false, parsed, @namespace));
    }
}
