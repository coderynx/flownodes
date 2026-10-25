using Xunit;

namespace Engine.IntegrationTests;

public sealed class NamespaceGrainTests
{
    private static readonly Guid Uid = Guid.NewGuid();

    [Fact]
    public async Task ApplyAsync_ShouldRequestResync_WhenGrainActivates()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();

        // Act
        await engine.Namespace("acme").ApplyAsync(Uid, 1);

        // Assert
        await engine.Registry.WaitForResyncsAsync("acme", 1, Ct);
        engine.Registry.NamespaceResyncs.ShouldBe(["acme"]);
    }

    [Fact]
    public async Task ApplyAsync_ShouldRegisterOneReminder_WhenNamespaceIsCreated()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();

        // Act
        await engine.Namespace("acme").ApplyAsync(Uid, 1);

        // Assert
        (await engine.HasReminderAsync("acme")).ShouldBeTrue();
    }

    [Fact]
    public async Task ApplyAsync_ShouldRequestResync_WhenSeqSkipsAhead()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        var grain = engine.Namespace("acme");
        await grain.ApplyAsync(Uid, 1);

        // Act
        await grain.ApplyAsync(Uid, 3);

        // Assert
        await engine.Registry.WaitForResyncsAsync("acme", 2, Ct);
        engine.Registry.Resyncs("acme").ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ApplyAsync_ShouldNotRequestResync_WhenSeqFollowsTheLastOne()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        var grain = engine.Namespace("acme");
        await grain.ApplyAsync(Uid, 1);

        // Act
        await grain.ApplyAsync(Uid, 2);
        await grain.ApplyAsync(Uid, 3);

        // Assert
        engine.Registry.Resyncs("acme").ShouldBe(1);
    }

    [Fact]
    public async Task ApplyAsync_ShouldIgnoreTheMessage_WhenSeqIsNotNewer()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        var grain = engine.Namespace("acme");
        await grain.ApplyAsync(Uid, 1);
        await grain.ApplyAsync(Uid, 2);

        // Act
        await grain.ApplyAsync(Uid, 2);
        await grain.ApplyAsync(Uid, 1);

        // Assert
        await grain.ApplyAsync(Uid, 3);
        engine.Registry.Resyncs("acme").ShouldBe(1);
    }

    [Fact]
    public async Task Grain_ShouldRequestResyncPeriodically_WhenItIsLive()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync(TimeSpan.FromMilliseconds(200));

        // Act
        await engine.Namespace("acme").ApplyAsync(Uid, 1);

        // Assert
        await engine.Registry.WaitForResyncsAsync("acme", 4, Ct);
        engine.Registry.Resyncs("acme").ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task Grain_ShouldActivateAgain_WhenItWasLostAndItsNamespaceExists()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.Namespace("acme").ApplyAsync(Uid, 1);
        await engine.Registry.WaitForResyncsAsync("acme", 1, Ct);

        // Act
        await engine.DeactivateAllAsync();

        // Assert
        await engine.Registry.WaitForResyncsAsync("acme", 2, Ct);
        engine.Registry.Resyncs("acme").ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldStopTheGrainComingBack_WhenItsNamespaceIsDeleted()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        var grain = engine.Namespace("acme");
        await grain.ApplyAsync(Uid, 1);
        await engine.Registry.WaitForResyncsAsync("acme", 1, Ct);

        // Act
        await grain.DeleteAsync(Uid, 2);
        await engine.DeactivateAllAsync();

        // Assert
        await Task.Delay(EngineCluster.ReminderPeriod * 3, Ct);
        engine.Registry.Resyncs("acme").ShouldBe(1);
        (await engine.HasReminderAsync("acme")).ShouldBeFalse();
    }

    [Fact]
    public async Task ApplyAsync_ShouldIgnoreTheMessage_WhenItIsStaleAboutADeletedNamespace()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        var grain = engine.Namespace("acme");
        await grain.ApplyAsync(Uid, 1);
        await grain.DeleteAsync(Uid, 2);

        // Act
        await grain.ApplyAsync(Uid, 1);
        await engine.DeactivateAllAsync();

        // Assert
        await Task.Delay(EngineCluster.ReminderPeriod * 3, Ct);
        engine.Registry.Resyncs("acme").ShouldBe(1);
        (await engine.HasReminderAsync("acme")).ShouldBeFalse();
    }

    [Fact]
    public async Task ApplyAsync_ShouldKeepTheGrainAlive_WhenNamespaceIsCreatedAgain()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        var grain = engine.Namespace("acme");
        await grain.ApplyAsync(Uid, 1);
        await grain.DeleteAsync(Uid, 2);
        await engine.Registry.WaitForResyncsAsync("acme", 1, Ct);

        // Act
        await grain.ApplyAsync(Guid.NewGuid(), 1);
        await engine.DeactivateAllAsync();

        // Assert
        await engine.Registry.WaitForResyncsAsync("acme", 2, Ct);
        engine.Registry.Resyncs("acme").ShouldBeGreaterThanOrEqualTo(2);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
