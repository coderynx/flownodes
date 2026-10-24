using Coderynx.Functional.Results.Errors;
using Coderynx.Functional.Results.Successes;
using Core.Application.Authorization;
using Core.Contracts.Resources;
using Registry.Application.Database;
using Registry.Application.Namespaces.DeleteNamespace;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Namespaces.DeleteNamespace;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenCallerIsUnknown()
    {
        // Arrange
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme"), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenCallerIsNotAnAdministrator()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))], users: [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "acme"), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.ForbiddenCode);
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenNameIsNotADnsLabel()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create(users: [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "Acme"), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.NotFound);
        result.Error.Code.ShouldBe("namespace.not_found");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenNamespaceDoesNotExist()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("plant-7"))], users: [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "acme"), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.NotFound);
        result.Error.Code.ShouldBe("namespace.not_found");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenNamespaceIsDefault()
    {
        // Arrange
        var caller = Callers.Administrator();
        var existing = Namespace.Create(NamespaceName.Create(NamespaceName.Default));
        var database = TestDatabase.Create([existing], users: [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, NamespaceName.Default), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
        result.Error.Code.ShouldBe("namespace.default_cannot_be_deleted");
        existing.Seq.ShouldBe(1);
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenNamespaceHoldsResources()
    {
        // Arrange
        var caller = Callers.Administrator();
        var name = NamespaceName.Create("acme");
        var existing = Namespace.Create(name);
        var database = TestDatabase.Create([existing], [Resource.Create(name, "Asset", "pump-1")], [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "acme"), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
        result.Error.Code.ShouldBe("namespace.holds_resources");
        existing.Seq.ShouldBe(1);
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldRemovePublishAndSave_WhenOnlyOtherNamespacesHoldResources()
    {
        // Arrange
        var caller = Callers.Administrator();
        var existing = Namespace.Create(NamespaceName.Create("acme"));
        var resource = Resource.Create(NamespaceName.Create("plant-7"), "Asset", "pump-1");
        var database = TestDatabase.Create([existing], [resource], [caller]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(caller.Id, "acme"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        existing.Seq.ShouldBe(2);
        database.Namespaces.Received(1).Remove(existing);
        await database.Received(1)
            .PublishAsync(
                Arg.Is<ResourceDeleted>(message =>
                    message.Name == "acme" && message.Uid == existing.Uid && message.Seq == 2));
        await database.Received(1).SaveChangesAsync(Ct);
    }

    private static async Task ShouldNotHaveDeletedAsync(IApplicationDbContext database)
    {
        database.Namespaces.DidNotReceive().Remove(Arg.Any<Namespace>());
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceDeleted>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
