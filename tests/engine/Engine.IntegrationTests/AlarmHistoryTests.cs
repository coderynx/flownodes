using Engine.Domain.Alarms;
using Engine.Infrastructure.Alarms;
using Npgsql;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class AlarmHistoryTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PurgeAsync_ShouldDeleteOnlyAlarmsThatEndedBeforeTheCutoff_WhenHistoryHoldsOldAndNewRows()
    {
        // Arrange
        var history = await StartAsync();
        var old = Ended("old", Now.AddDays(-31));
        var recent = Ended("recent", Now.AddDays(-29));
        var live = Alarm.Raise("live", Condition(), Now.AddDays(-90));
        await history.UpsertAsync("acme", [old, recent, live], Ct);

        // Act
        await history.PurgeAsync(Now.AddDays(-30), Ct);

        // Assert
        var page = await history.PageAsync("acme", null, null, null, null, null, 0, 10, Ct);
        page.Items.Select(alarm => alarm.Asset).Order().ShouldBe(["live", "recent"]);
    }

    [Fact]
    public async Task PageAsync_ShouldReturnNewestFirstAndHonourTheFilters_WhenHistoryHoldsSeveralAssets()
    {
        // Arrange
        var history = await StartAsync();
        var first = Alarm.Raise("a", Condition(), Now);
        var second = Alarm.Raise("b", Condition(), Now.AddHours(1));
        var third = Alarm.Raise("a", Condition(), Now.AddHours(2));
        await history.UpsertAsync("acme", [first, second, third], Ct);
        await history.UpsertAsync("other", [Alarm.Raise("a", Condition(), Now.AddHours(3))], Ct);

        // Act
        var everything = await history.PageAsync("acme", null, null, null, null, null, 0, 10, Ct);
        var second_page = await history.PageAsync("acme", null, null, null, null, null, 1, 1, Ct);
        var assetA = await history.PageAsync("acme", "a", null, null, null, null, 0, 10, Ct);
        var window = await history.PageAsync(
            "acme",
            null,
            Now.AddMinutes(30),
            Now.AddHours(1.5),
            null,
            null,
            0,
            10,
            Ct);

        // Assert
        everything.Items.Select(alarm => alarm.Id).ShouldBe([third.Id, second.Id, first.Id]);
        everything.Total.ShouldBe(3);
        second_page.Items.Single().Id.ShouldBe(second.Id);
        second_page.Total.ShouldBe(3);
        assetA.Items.Count.ShouldBe(2);
        window.Items.Single().Id.ShouldBe(second.Id);
    }

    [Fact]
    public async Task UpsertAsync_ShouldUpdateTheRowAtEachTransition_WhenTheAlarmChanges()
    {
        // Arrange
        var history = await StartAsync();
        var alarm = Alarm.Raise("a", Condition(), Now);
        await history.UpsertAsync("acme", [alarm], Ct);
        var user = Guid.NewGuid();

        // Act
        alarm.Clear(Now.AddMinutes(1));
        alarm.Acknowledge(user, "ada", "done", Now.AddMinutes(2));
        await history.UpsertAsync("acme", [alarm], Ct);

        // Assert
        var row = (await history.PageAsync("acme", null, null, null, null, null, 0, 10, Ct)).Items.Single();
        row.AcknowledgedBy.ShouldBe(user);
        row.AcknowledgedByName.ShouldBe("ada");
        row.AcknowledgementComment.ShouldBe("done");
        row.ClearedAt.ShouldBe(Now.AddMinutes(1));
        row.AcknowledgedAt.ShouldBe(Now.AddMinutes(2));
        row.EndedAt.ShouldBe(Now.AddMinutes(2));
        row.Outcome.ShouldBe(AlarmOutcome.Ended);
        (await history.ListLiveAsync("acme", Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ListLiveAsync_ShouldPutCriticalFirstThenTheEarliestRaised_WhenAlarmsDiffer()
    {
        // Arrange
        var history = await StartAsync();
        var lowEarly = Alarm.Raise("a", Condition(AlarmPriority.Low), Now);
        var criticalLate = Alarm.Raise("b", Condition(AlarmPriority.Critical), Now.AddHours(1));
        var lowLate = Alarm.Raise("c", Condition(AlarmPriority.Low), Now.AddHours(2));
        await history.UpsertAsync("acme", [lowLate, lowEarly, criticalLate], Ct);

        // Act
        var live = await history.ListLiveAsync("acme", Ct);

        // Assert
        live.Select(alarm => alarm.Id).ShouldBe([criticalLate.Id, lowEarly.Id, lowLate.Id]);
    }

    private async Task<PostgresAlarmHistory> StartAsync()
    {
        var database = NpgsqlDataSource.Create(await postgres.CreateDatabaseAsync());
        await new AlarmSchema(database).StartAsync(Ct);

        return new PostgresAlarmHistory(database);
    }

    private static Alarm Ended(string asset, DateTimeOffset endedAt)
    {
        var alarm = Alarm.Raise(asset, Condition(), endedAt.AddHours(-1));
        alarm.Retire(endedAt);

        return alarm;
    }

    private static AlarmCondition Condition(AlarmPriority priority = AlarmPriority.High) =>
        new("low", "pressure", "<", "3", null, null, priority, "Brake pressure below 3 bar");
}
