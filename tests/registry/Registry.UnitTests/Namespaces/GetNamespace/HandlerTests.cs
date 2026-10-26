using Coderynx.Functional.Results.Errors;
using Coderynx.Functional.Results.Successes;
using Core.Application.Authorization;
using Core.Domain.Authorization;
using Registry.Application.Namespaces.GetNamespace;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Namespaces.GetNamespace;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnTheError_WhenTheAccessCheckFails()
    {
        // Arrange
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))]);
        var handler = new Handler(database, AccessDoubles.ChecksFailingWith(AccessDoubles.Unauthenticated()));

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid(), "acme"), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenNameIsNotADnsLabel()
    {
        // Arrange
        var caller = Callers.Member("Acme");
        var database = TestDatabase.Create();
        var handler = new Handler(database, AccessDoubles.ChecksFor(caller));

        // Act
        var result = await handler.HandleAsync(new Query(caller.Id, "Acme"), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.NotFound);
        result.Error.Code.ShouldBe("namespace.not_found");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenMemberNamespaceIsNotStored()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create();
        var handler = new Handler(database, AccessDoubles.ChecksFor(caller));

        // Act
        var result = await handler.HandleAsync(new Query(caller.Id, "acme"), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.NotFound);
        result.Error.Code.ShouldBe("namespace.not_found");
    }

    [Theory]
    [InlineData(NamespaceRole.Operator)]
    [InlineData(NamespaceRole.Engineer)]
    public async Task HandleAsync_ShouldReturnNamespace_WhenCallerHoldsRole(NamespaceRole role)
    {
        // Arrange
        var caller = Callers.Member("acme", role);
        var acme = Namespace.Create(NamespaceName.Create("acme"));
        var other = Namespace.Create(NamespaceName.Create("plant-7"));
        var database = TestDatabase.Create([other, acme]);
        var handler = new Handler(database, AccessDoubles.ChecksFor(caller));

        // Act
        var result = await handler.HandleAsync(new Query(caller.Id, "acme"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        result.Value.ShouldBeSameAs(acme);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
