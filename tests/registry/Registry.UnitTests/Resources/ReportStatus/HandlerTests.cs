using Registry.Application.Resources.ReportStatus;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Resources.ReportStatus;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldKeepTheStatusAndSave_WhenItMatchesTheResource()
    {
        // Arrange
        var widget = Widgets.Stored("acme", "w-1");
        var database = TestDatabase.Create(resources: [widget]);

        // Act
        await new Handler(database).HandleAsync(Report(widget.Uid, 1), Ct);

        // Assert
        widget.ObservedGeneration.ShouldBe(1);
        widget.Status.ShouldNotBeNull();
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveNothing_WhenTheStatusIsAboutAnotherIncarnation()
    {
        // Arrange
        var widget = Widgets.Stored("acme", "w-1");
        var database = TestDatabase.Create(resources: [widget]);

        // Act
        await new Handler(database).HandleAsync(Report(Guid.NewGuid(), 1), Ct);

        // Assert
        widget.Status.ShouldBeNull();
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveNothing_WhenTheResourceIsGone()
    {
        // Arrange
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))]);

        // Act
        await new Handler(database).HandleAsync(Report(Guid.NewGuid(), 1), Ct);

        // Assert
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Command Report(Guid uid, long statusSeq) =>
        new("acme", WidgetKind.Kind, "w-1", uid, 1, statusSeq, Widgets.Json("""{"conditions": []}"""));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
