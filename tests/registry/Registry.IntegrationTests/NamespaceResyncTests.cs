using Core.Contracts.Resources;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class NamespaceResyncTests(PostgresFixture postgres)
{
    [Fact]
    public async Task NamespaceResyncRequested_ShouldPublishEveryResourceInTheNamespaceAgain_WhenReceived()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);
        await app.JoinAsync("acme", Ct);
        await app.GrantAsync(app.AdministratorId, "Engineer", "acme");
        await RegistryApp.EventuallyAsync(
            async () => (await RegistryApp.ApplyAsync(
                app.Client,
                """
                apiVersion: flownodes.io/v1alpha1
                kind: Widget
                metadata:
                  name: w-1
                spec: {}
                """,
                Ct,
                "?namespace=acme")).IsSuccessStatusCode,
            Ct);
        var first = await app.Messages.WaitForAppliedAsync("w-1", Ct);

        // Act
        await app.PublishAsync(new NamespaceResyncRequested("acme"));

        // Assert
        await app.Messages.WaitForAppliedCountAsync("w-1", 2, Ct);
        var again = app.Messages.Received.OfType<ResourceApplied>().Last(applied => applied.Name == "w-1");
        (again.Uid, again.Generation, again.Seq).ShouldBe((first.Uid, first.Generation, first.Seq));
    }

    [Fact]
    public async Task NamespaceResyncRequested_ShouldPublishOnlyThatNamespaceAgain_WhenReceived()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);
        await app.CreateNamespaceAsync("globex", Ct);
        await app.Messages.WaitForAppliedAsync("globex", Ct);
        await app.Messages.WaitForAppliedAsync("acme", Ct);

        // Act
        await app.PublishAsync(new NamespaceResyncRequested("acme"));

        // Assert
        await app.Messages.WaitForAppliedCountAsync("acme", 2, Ct);
        app.Messages.Received.OfType<ResourceApplied>().Count(applied => applied.Name == "globex").ShouldBe(1);
    }

    [Fact]
    public async Task NamespaceResyncRequested_ShouldPublishNothing_WhenNamespaceDoesNotExist()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        await app.PublishAsync(new NamespaceResyncRequested("ghost"));

        // Assert
        await app.PublishAsync(new NamespaceResyncRequested("default"));
        await app.Messages.WaitForAppliedCountAsync("default", 2, Ct);
        app.Messages.Received.OfType<ResourceApplied>().ShouldNotContain(applied => applied.Name == "ghost");
    }

    [Fact]
    public async Task NamespacesListRequested_ShouldPublishTheNameOfEveryNamespace_WhenReceived()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);
        await app.Messages.WaitForAppliedAsync("acme", Ct);

        // Act
        await app.PublishAsync(new NamespacesListRequested());

        // Assert
        var listed = await app.Messages.WaitForListedAsync(Ct);
        listed.Names.ShouldBe(["acme", "default"], ignoreOrder: true);
    }

    [Fact]
    public async Task NamespacesListRequested_ShouldPublishAnEmptyList_WhenNoNamespaceExists()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.DeleteEveryNamespaceAsync(Ct);

        // Act
        await app.PublishAsync(new NamespacesListRequested());

        // Assert
        var listed = await app.Messages.WaitForListedAsync(Ct);
        listed.Names.ShouldBeEmpty();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
