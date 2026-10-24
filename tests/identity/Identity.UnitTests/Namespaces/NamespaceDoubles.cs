using Identity.Domain.Users;

namespace Identity.UnitTests.Namespaces;

internal static class NamespaceDoubles
{
    public static readonly Guid Uid = Guid.NewGuid();

    public static User UserWith(string username, params (string Namespace, Role Role)[] grants)
    {
        var user = TestDoubles.Member(username);
        foreach (var (@namespace, role) in grants)
        {
            user.Grant(@namespace, role);
        }

        return user;
    }
}
