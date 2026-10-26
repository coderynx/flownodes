using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Core.Application.Authorization;
using Core.Domain.Authorization;
using Registry.Application.Resources;
using Registry.Domain.Namespaces;

namespace Registry.UnitTests;

internal static class AccessDoubles
{
    public static Error Unauthenticated() => Error.Custom(AccessErrors.UnauthenticatedCode, "Not an enabled User.");
    public static Error NamespaceNotFound() => Error.NotFound("namespace.not_found", "Namespace does not exist.");

    public static IAccessChecks ChecksFor(UserAccess caller)
    {
        var checks = Substitute.For<IAccessChecks>();
        var names = caller.Grants.Select(grant => grant.Namespace).ToHashSet();
        checks.ScopeAsync(caller.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Found(new NamespaceScope(caller.IsAdministrator, names)));
        checks.RequireMemberAsync(caller.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Found(caller));

        return checks;
    }

    public static IAccessChecks ChecksFailingWith(Error error)
    {
        var checks = Substitute.For<IAccessChecks>();
        checks.ScopeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(error);
        checks.RequireMemberAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(error);

        return checks;
    }

    public static IResourceAccess ReadsFor(string @namespace)
    {
        var access = Substitute.For<IResourceAccess>();
        access.AuthoriseReadAsync(Arg.Any<Guid>(), @namespace, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => WidgetKind.Catalog.FindByRoute(call.ArgAt<string>(2)) is { } kind
                ? Result.Found((kind, NamespaceName.Create(@namespace)))
                : Errors.KindNotFound(call.ArgAt<string>(2)));

        return access;
    }

    public static IResourceAccess ReadsFailingWith(Error error)
    {
        var access = Substitute.For<IResourceAccess>();
        access.AuthoriseReadAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(error);

        return access;
    }
}
