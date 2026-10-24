using Identity.Application.Database;
using Identity.Domain.Namespaces;
using Identity.Domain.Users;
using MockQueryable.NSubstitute;

namespace Identity.UnitTests.Namespaces;

internal static class NamespaceDoubles
{
    public static readonly Guid Uid = Guid.NewGuid();

    public static IIdentityDbContext Database(IEnumerable<KnownNamespace> namespaces, IEnumerable<User> users)
    {
        var database = Substitute.For<IIdentityDbContext>();
        var namespaceSet = namespaces.ToList().BuildMockDbSet();
        var userSet = users.ToList().BuildMockDbSet();
        database.KnownNamespaces.Returns(namespaceSet);
        database.Users.Returns(userSet);

        return database;
    }

    public static User UserWith(string username, params (string Namespace, Role Role)[] grants)
    {
        var user = User.Create(Username.Create(username), null);
        foreach (var (@namespace, role) in grants)
        {
            user.Grant(@namespace, role);
        }

        return user;
    }
}
