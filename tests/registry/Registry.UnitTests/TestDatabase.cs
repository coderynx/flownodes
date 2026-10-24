using Core.Domain.Authorization;
using MockQueryable.NSubstitute;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.UnitTests;

internal static class TestDatabase
{
    public static IApplicationDbContext Create(
        List<Namespace>? namespaces = null,
        List<Resource>? resources = null,
        List<UserAccess>? users = null)
    {
        var database = Substitute.For<IApplicationDbContext>();
        var namespaceSet = (namespaces ?? []).BuildMockDbSet();
        var resourceSet = (resources ?? []).BuildMockDbSet();
        var userSet = (users ?? []).BuildMockDbSet();
        database.Namespaces.Returns(namespaceSet);
        database.Resources.Returns(resourceSet);
        database.UserAccess.Returns(userSet);

        return database;
    }
}
