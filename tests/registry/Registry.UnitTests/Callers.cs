using Core.Domain.Authorization;

namespace Registry.UnitTests;

internal static class Callers
{
    public static UserAccess Administrator()
    {
        var caller = UserAccess.Create(Guid.NewGuid());
        caller.GrantAdministrator(1);

        return caller;
    }

    public static UserAccess Member(string @namespace, NamespaceRole role = NamespaceRole.Operator)
    {
        var caller = UserAccess.Create(Guid.NewGuid());
        caller.GrantRole(@namespace, role, 1);

        return caller;
    }

    public static UserAccess DisabledAdministrator()
    {
        var caller = Administrator();
        caller.Disable(2);

        return caller;
    }
}
