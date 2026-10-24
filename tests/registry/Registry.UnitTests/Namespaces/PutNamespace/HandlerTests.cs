using Coderynx.Functional.Results.Errors;
using Coderynx.Functional.Results.Successes;
using Core.Application.Authorization;
using Core.Contracts.Resources;
using Core.Domain.Authorization;
using Registry.Application.Namespaces.PutNamespace;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Namespaces.PutNamespace;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenCallerIsUnknown()
    {
        // Arrange
        var database = TestDatabase.Create();
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme"), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenCallerIsDisabled()
    {
        // Arrange
        var caller = Callers.DisabledAdministrator();
        var database = TestDatabase.Create(users: [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "acme"), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenCallerIsNotAnAdministrator()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var database = TestDatabase.Create(users: [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "plant-7"), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.ForbiddenCode);
        database.Namespaces.DidNotReceive().Add(Arg.Any<Namespace>());
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceApplied>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnInvalidInput_WhenNameIsNotADnsLabel()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create(users: [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "Acme"), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.InvalidInput);
        result.Error.Code.ShouldBe("namespace.invalid_name");
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFound_WhenNamespaceExists()
    {
        // Arrange
        var caller = Callers.Administrator();
        var existing = Namespace.Create(NamespaceName.Create("acme"));
        var database = TestDatabase.Create([existing], users: [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "acme"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        result.Value.ShouldBeSameAs(existing);
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceApplied>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldCreatePublishAndSave_WhenNamespaceIsNew()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create(users: [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "acme"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Created);
        result.Value.Name.Value.ShouldBe("acme");
        database.Namespaces.Received(1).Add(result.Value);
        await database.Received(1)
            .PublishAsync(
                Arg.Is<ResourceApplied>(message =>
                    message.Namespace == "acme" && message.Kind == Namespace.Kind && message.Uid == result.Value.Uid));
        await database.Received(1).SaveChangesAsync(Ct);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
