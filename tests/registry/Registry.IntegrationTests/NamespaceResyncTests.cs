using Core.Contracts.Resources;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class NamespaceResyncTests(PostgresFixture postgres)
{
    [Fact]
    public async Task NamespacesResyncRequested_ShouldPublishEveryNamespaceAgain_WhenNoNamespaceIsNamed()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);
        await app.Messages.WaitForAppliedAsync("acme", Ct);
        await app.Messages.WaitForAppliedAsync("default", Ct);

        // Act
        await app.PublishAsync(new NamespacesResyncRequested());

        // Assert
        await app.Messages.WaitForAppliedCountAsync("acme", 2, Ct);
        await app.Messages.WaitForAppliedCountAsync("default", 2, Ct);
        var again = app.Messages.Received.OfType<ResourceApplied>().Last(applied => applied.Name == "acme");
        again.Kind.ShouldBe("Namespace");
        again.Seq.ShouldBe(1);
    }

    [Fact]
    public async Task NamespacesResyncRequested_ShouldPublishOnlyThatNamespaceAgain_WhenReceived()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);
        await app.CreateNamespaceAsync("globex", Ct);
        await app.Messages.WaitForAppliedAsync("globex", Ct);
        await app.Messages.WaitForAppliedAsync("acme", Ct);

        // Act
        await app.PublishAsync(new NamespacesResyncRequested("acme"));

        // Assert
        await app.Messages.WaitForAppliedCountAsync("acme", 2, Ct);
        app.Messages.Received.OfType<ResourceApplied>().Count(applied => applied.Name == "globex").ShouldBe(1);
    }

    [Fact]
    public async Task NamespacesResyncRequested_ShouldPublishNothing_WhenNamespaceDoesNotExist()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        await app.PublishAsync(new NamespacesResyncRequested("ghost"));

        // Assert
        await app.PublishAsync(new NamespacesResyncRequested("default"));
        await app.Messages.WaitForAppliedCountAsync("default", 2, Ct);
        app.Messages.Received.OfType<ResourceApplied>().ShouldNotContain(applied => applied.Name == "ghost");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
