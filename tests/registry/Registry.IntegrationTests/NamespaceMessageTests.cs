using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class NamespaceMessageTests(PostgresFixture postgres)
{
    private const string Route = "/api/v1/registry/namespaces";

    [Fact]
    public async Task Startup_ShouldPublishResourceApplied_WhenDefaultNamespaceIsSeeded()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var applied = await app.Messages.WaitForAppliedAsync("default", Ct);

        // Assert
        applied.Kind.ShouldBe("Namespace");
        applied.Namespace.ShouldBe("default");
        applied.Seq.ShouldBe(1);
    }

    [Fact]
    public async Task ApplyNamespace_ShouldPublishResourceApplied_WhenNamespaceIsCreated()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);

        // Assert
        var applied = await app.Messages.WaitForAppliedAsync("acme", Ct);
        applied.Kind.ShouldBe("Namespace");
        applied.Namespace.ShouldBe("acme");
        applied.Uid.ShouldNotBe(Guid.Empty);
        applied.Generation.ShouldBe(1);
        applied.Seq.ShouldBe(1);
        applied.Spec.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public async Task DeleteNamespace_ShouldPublishResourceDeletedWithTheNextSeq_WhenNamespaceIsDeleted()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);

        // Act
        await app.Client.DeleteAsync($"{Route}/acme", Ct);

        // Assert
        var deleted = await app.Messages.WaitForDeletedAsync("acme", Ct);
        var applied = await app.Messages.WaitForAppliedAsync("acme", Ct);
        deleted.Kind.ShouldBe("Namespace");
        deleted.Uid.ShouldBe(applied.Uid);
        deleted.Seq.ShouldBe(applied.Seq + 1);
    }

    [Fact]
    public async Task ApplyNamespace_ShouldContinueTheDeletedNamespacesSeq_WhenNamespaceIsRecreated()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);
        await app.Client.DeleteAsync($"{Route}/acme", Ct);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);
        await app.Client.DeleteAsync($"{Route}/acme", Ct);

        // Act
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);

        // Assert
        await app.Messages.WaitForAppliedCountAsync("acme", 3, Ct);
        await app.Messages.WaitForDeletedCountAsync("acme", 2, Ct);
        var seqs = app.Messages.Received.OfType<Core.Contracts.Resources.ResourceApplied>()
            .Where(applied => applied.Name == "acme")
            .Select(applied => applied.Seq)
            .Concat(
                app.Messages.Received.OfType<Core.Contracts.Resources.ResourceDeleted>()
                    .Where(deleted => deleted.Name == "acme")
                    .Select(deleted => deleted.Seq))
            .Order();
        seqs.ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task ApplyNamespace_ShouldPublishNothing_WhenNamespaceExists()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);

        // Act
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);

        // Assert
        await RegistryApp.ApplyNamespaceAsync(app.Client, "marker", Ct);
        await app.Messages.WaitForAppliedAsync("marker", Ct);
        app.Messages.Received.OfType<Core.Contracts.Resources.ResourceApplied>()
            .Count(applied => applied.Name == "acme")
            .ShouldBe(1);
    }

    [Fact]
    public async Task DeleteNamespace_ShouldPublishNothing_WhenDeleteIsRejected()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            database.Resources.Add(
                Resource.Create(
                    NamespaceName.Create("acme"),
                    "Connection",
                    ResourceName.Create("broker"),
                    JsonSerializer.SerializeToElement(new { }),
                    2));
            await database.SaveChangesAsync(Ct);
        }

        // Act
        await app.Client.DeleteAsync($"{Route}/acme", Ct);
        await app.Client.DeleteAsync($"{Route}/default", Ct);

        // Assert
        await RegistryApp.ApplyNamespaceAsync(app.Client, "marker", Ct);
        await app.Messages.WaitForAppliedAsync("marker", Ct);
        app.Messages.Received.OfType<Core.Contracts.Resources.ResourceDeleted>().ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChanges_ShouldSendNothing_WhenTheTransactionRollsBack()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        database.Namespaces.Add(Namespace.Create(NamespaceName.Create("acme")));
        await database.PublishAsync(
            new Core.Contracts.Resources.ResourceApplied(
                "ghost",
                Namespace.Kind,
                "ghost",
                Guid.NewGuid(),
                1,
                1,
                JsonSerializer.SerializeToElement(new { })));

        // Act
        var save = () => database.SaveChangesAsync(Ct);

        // Assert
        await Should.ThrowAsync<DbUpdateException>(save);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "marker", Ct);
        await app.Messages.WaitForAppliedAsync("marker", Ct);
        app.Messages.Received.OfType<Core.Contracts.Resources.ResourceApplied>()
            .ShouldNotContain(applied => applied.Name == "ghost");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
