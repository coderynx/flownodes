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
        List<UserAccess>? users = null,
        List<Revision>? revisions = null,
        List<SecretValue>? secretValues = null)
    {
        var database = Substitute.For<IApplicationDbContext>();
        var namespaceSet = (namespaces ?? []).BuildMockDbSet();
        var resourceSet = (resources ?? []).BuildMockDbSet();
        var userSet = (users ?? []).BuildMockDbSet();
        var revisionSet = (revisions ?? []).BuildMockDbSet();
        var secretValueSet = (secretValues ?? []).BuildMockDbSet();
        var deletedNamespaceSet = new List<DeletedNamespace>().BuildMockDbSet();
        database.SecretValues.Returns(secretValueSet);
        database.Namespaces.Returns(namespaceSet);
        database.DeletedNamespaces.Returns(deletedNamespaceSet);
        database.Resources.Returns(resourceSet);
        database.UserAccess.Returns(userSet);
        database.Revisions.Returns(revisionSet);

        return database;
    }
}
