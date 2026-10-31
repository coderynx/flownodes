using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Engine.Domain.Alarms;
using Engine.Infrastructure.Alarms;
using Npgsql;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class AlarmEndpointTests(PostgresFixture postgres)
{
    private const string Api = "/api/v1/engine/namespaces/acme";
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string Train = """
                                 {"dataPoints": [{"name": "pressure", "type": "number"}],
                                  "alarms": [{"name": "low", "dataPoint": "pressure", "operator": "<", "value": 3,
                                              "priority": "high", "message": "Brake pressure below 3 bar"}]}
                                 """;

    [Theory]
    [InlineData("Operator")]
    [InlineData("Engineer")]
    public async Task ListAlarms_ShouldSucceed_WhenTheCallerHoldsAnyRole(string role)
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, member) = await app.MemberAsync(role, "acme");

        // Act
        var live = await member.GetAsync($"{Api}/alarms", Ct);
        var history = await member.GetAsync($"{Api}/alarms/history", Ct);

        // Assert
        live.StatusCode.ShouldBe(HttpStatusCode.OK);
        history.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldRecordWhoAndWhen_WhenTheCallerIsAnOperator()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (userId, member) = await app.MemberAsync("Operator", "acme");
        var id = await RaiseAsync(app, member);

        // Act
        var response = await member.PostAsJsonAsync($"{Api}/alarms/{id}/acknowledge", new { comment = "on it" }, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var live = await ItemsAsync(member, $"{Api}/assets/train/alarms");
        live.Single().GetProperty("acknowledgedBy").GetGuid().ShouldBe(userId);
        live.Single().GetProperty("acknowledgedByName").GetString().ShouldBe("ada");
        live.Single().GetProperty("dataPoint").GetString().ShouldBe("pressure");
        live.Single().GetProperty("acknowledgementComment").GetString().ShouldBe("on it");
        live.Single().GetProperty("state").GetString().ShouldBe("active");
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldReturnForbidden_WhenTheCallerIsAnEngineerWithoutTheOperatorRole()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");
        var (_, engineer) = await app.MemberAsync("Engineer", "acme");
        var id = await RaiseAsync(app, operatorClient);

        // Act
        var response = await engineer.PostAsync($"{Api}/alarms/{id}/acknowledge", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ItemsAsync(operatorClient, $"{Api}/alarms")).Single()
            .GetProperty("acknowledgedAt")
            .ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldReturnUnauthorized_WhenTheTokenCarriesNoName()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, nameless) = await app.MemberAsync("Operator", "acme", null);
        var id = await RaiseAsync(app, nameless);

        // Act
        var response = await nameless.PostAsync($"{Api}/alarms/{id}/acknowledge", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldKeepTheFirstAcknowledgement_WhenTheSameUserRepeatsItUnderANewName()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (userId, first) = await app.MemberAsync("Operator", "acme");
        var id = await RaiseAsync(app, first);
        await first.PostAsJsonAsync($"{Api}/alarms/{id}/acknowledge", new { comment = "on it" }, Ct);
        var renamed = app.SignedIn(userId, "ada.lovelace");

        // Act
        var response = await renamed.PostAsJsonAsync($"{Api}/alarms/{id}/acknowledge", new { comment = "again" }, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var alarm = await first.GetFromJsonAsync<JsonElement>($"{Api}/alarms/{id}", Ct);
        alarm.GetProperty("acknowledgedByName").GetString().ShouldBe("ada");
        alarm.GetProperty("acknowledgementComment").GetString().ShouldBe("on it");
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldReturnConflictNamingTheFirstAcknowledger_WhenAnotherUserTries()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, first) = await app.MemberAsync("Operator", "acme");
        var (_, second) = await app.MemberAsync("Operator", "acme", "grace");
        var id = await RaiseAsync(app, first);
        await first.PostAsJsonAsync($"{Api}/alarms/{id}/acknowledge", new { comment = "on it" }, Ct);

        // Act
        var response = await second.PostAsJsonAsync($"{Api}/alarms/{id}/acknowledge", new { comment = "me too" }, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        problem.GetProperty("detail").GetString()!.ShouldContain("ada");
        var alarm = await first.GetFromJsonAsync<JsonElement>($"{Api}/alarms/{id}", Ct);
        alarm.GetProperty("acknowledgementComment").GetString().ShouldBe("on it");
    }

    [Theory]
    [InlineData("ada", HttpStatusCode.NoContent)]
    [InlineData("grace", HttpStatusCode.Conflict)]
    public async Task AcknowledgeAlarm_ShouldAnswerByWhoAcknowledged_WhenTheAcknowledgementEndedTheAlarm(
        string caller,
        HttpStatusCode expected)
    {
        // Arrange
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var app = await EngineApp.StartAsync(connectionString);
        var (first, firstClient) = await app.MemberAsync("Operator", "acme");
        var (_, secondClient) = await app.MemberAsync("Operator", "acme", "grace");
        var ended = Alarm.Raise("train", LowPressure(), Now);
        ended.Clear(Now.AddMinutes(1));
        ended.Acknowledge(first, "ada", "done", Now.AddMinutes(2));
        await History(connectionString).UpsertAsync("acme", [ended], Ct);
        var client = caller == "ada" ? firstClient : secondClient;

        // Act
        var response = await client.PostAsync($"{Api}/alarms/{ended.Id}/acknowledge", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldLetOneCallerWin_WhenTwoRaceToEndAClearedAlarm()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, first) = await app.MemberAsync("Operator", "acme");
        var (_, second) = await app.MemberAsync("Operator", "acme", "grace");
        var id = await RaiseAsync(app, first);
        await first.PutAsync(
            $"{Api}/assets/train/datapoints/pressure",
            new StringContent("""{"value": 4}""", Encoding.UTF8, "application/json"),
            Ct);
        await Wait.UntilAsync(
            async () => (await first.GetFromJsonAsync<JsonElement>($"{Api}/alarms/{id}", Ct)).GetProperty("state")
                .GetString() == "cleared"
                    ? "cleared"
                    : null,
            Ct);

        // Act
        var responses = await Task.WhenAll(
            first.PostAsync($"{Api}/alarms/{id}/acknowledge", null, Ct),
            second.PostAsync($"{Api}/alarms/{id}/acknowledge", null, Ct));

        // Assert
        responses.Select(response => response.StatusCode)
            .Order()
            .ShouldBe([HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldReturnNotFound_WhenTheAlarmWasRetired()
    {
        // Arrange
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var app = await EngineApp.StartAsync(connectionString);
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");
        var retired = Alarm.Raise("train", LowPressure(), Now);
        retired.Retire(Now.AddMinutes(1));
        await History(connectionString).UpsertAsync("acme", [retired], Ct);

        // Act
        var response = await operatorClient.PostAsync($"{Api}/alarms/{retired.Id}/acknowledge", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("Operator")]
    [InlineData("Engineer")]
    public async Task GetAlarm_ShouldReturnAnEndedAlarm_WhenTheCallerHoldsAnyRole(string role)
    {
        // Arrange
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var app = await EngineApp.StartAsync(connectionString);
        var (_, member) = await app.MemberAsync(role, "acme");
        var retired = Alarm.Raise("train", LowPressure(), Now);
        retired.Retire(Now.AddMinutes(1));
        await History(connectionString).UpsertAsync("acme", [retired], Ct);

        // Act
        var found = await member.GetFromJsonAsync<JsonElement>($"{Api}/alarms/{retired.Id}", Ct);

        // Assert
        found.GetProperty("state").GetString().ShouldBe("retired");
    }

    [Fact]
    public async Task GetAlarm_ShouldReturnNotFound_WhenTheAlarmWasPurged()
    {
        // Arrange
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var app = await EngineApp.StartAsync(connectionString);
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");
        var retired = Alarm.Raise("train", LowPressure(), Now);
        retired.Retire(Now.AddMinutes(1));
        var history = History(connectionString);
        await history.UpsertAsync("acme", [retired], Ct);
        await history.PurgeAsync(Now.AddDays(1), Ct);

        // Act
        var response = await operatorClient.GetAsync($"{Api}/alarms/{retired.Id}", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAlarm_ShouldReturnALiveAlarm_WhenItIsActive()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");
        var id = await RaiseAsync(app, operatorClient);

        // Act
        var alarm = await operatorClient.GetFromJsonAsync<JsonElement>($"{Api}/alarms/{id}", Ct);

        // Assert
        alarm.GetProperty("state").GetString().ShouldBe("active");
    }

    [Fact]
    public async Task GetAlarm_ShouldReturnNotFound_WhenTheCallerIsNotAMemberOfTheNamespace()
    {
        // Arrange
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var app = await EngineApp.StartAsync(connectionString);
        var (_, outsider) = await app.MemberAsync("Operator", "other");
        var alarm = Alarm.Raise("train", LowPressure(), Now);
        await History(connectionString).UpsertAsync("acme", [alarm], Ct);

        // Act
        var response = await outsider.GetAsync($"{Api}/alarms/{alarm.Id}", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAlarm_ShouldLeaveOutTheDataPoint_WhenNoAlarmConditionRaisedTheAlarm()
    {
        // Arrange
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var app = await EngineApp.StartAsync(connectionString);
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");
        var alarm = Alarm.Restore(
            Guid.NewGuid(),
            "train",
            "raised-by-flow",
            null,
            string.Empty,
            Now,
            AlarmPriority.High,
            "Doors stuck",
            1,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        await History(connectionString).UpsertAsync("acme", [alarm], Ct);

        // Act
        var body = await operatorClient.GetFromJsonAsync<JsonElement>($"{Api}/alarms/{alarm.Id}", Ct);

        // Assert
        body.TryGetProperty("dataPoint", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("priority=critical", "critical-active")]
    [InlineData("state=active", "critical-active")]
    [InlineData("state=cleared", "low-cleared")]
    [InlineData("state=ended", "low-ended")]
    [InlineData("state=retired", "low-retired-while-active")]
    [InlineData("priority=critical&state=retired", null)]
    public async Task ListAlarmHistory_ShouldFilterByPriorityAndState_WhenAsked(string filter, string? expectedAsset)
    {
        // Arrange
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var app = await EngineApp.StartAsync(connectionString);
        var (user, operatorClient) = await app.MemberAsync("Operator", "acme");
        var active = Alarm.Raise("critical-active", LowPressure(AlarmPriority.Critical), Now);
        var cleared = Alarm.Raise("low-cleared", LowPressure(AlarmPriority.Low), Now);
        cleared.Clear(Now.AddMinutes(1));
        var ended = Alarm.Raise("low-ended", LowPressure(AlarmPriority.Low), Now);
        ended.Clear(Now.AddMinutes(1));
        ended.Acknowledge(user, "ada", null, Now.AddMinutes(2));
        var retired = Alarm.Raise("low-retired-while-active", LowPressure(AlarmPriority.Low), Now);
        retired.Retire(Now.AddMinutes(1));
        await History(connectionString).UpsertAsync("acme", [active, cleared, ended, retired], Ct);

        // Act
        var page = await operatorClient.GetFromJsonAsync<JsonElement>($"{Api}/alarms/history?{filter}", Ct);

        // Assert
        page.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("asset").GetString())
            .ShouldBe(expectedAsset is null ? [] : [expectedAsset]);
    }

    [Theory]
    [InlineData("priority=urgent")]
    [InlineData("state=gone")]
    public async Task ListAlarmHistory_ShouldReturnBadRequest_WhenAFilterValueIsUnknown(string filter)
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");

        // Act
        var response = await operatorClient.GetAsync($"{Api}/alarms/history?{filter}", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldAcceptNoBody_WhenThereIsNoComment()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");
        var id = await RaiseAsync(app, operatorClient);

        // Act
        var response = await operatorClient.PostAsync($"{Api}/alarms/{id}/acknowledge", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldReturnNotFound_WhenTheAlarmIsUnknown()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");

        // Act
        var response = await operatorClient.PostAsync($"{Api}/alarms/{Guid.NewGuid()}/acknowledge", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcknowledgeAlarm_ShouldReturnNotFound_WhenTheCallerIsNotAMemberOfTheNamespace()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");
        var id = await RaiseAsync(app, operatorClient);
        var (_, outsider) = await app.MemberAsync("Operator", "other");

        // Act
        var response = await outsider.PostAsync($"{Api}/alarms/{id}/acknowledge", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ItemsAsync(operatorClient, $"{Api}/alarms")).Single()
            .GetProperty("acknowledgedAt")
            .ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task ListAlarmHistory_ShouldPageAndFilterByAssetAndTime_WhenAlarmsWereRaised()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");
        await RaiseAsync(app, operatorClient);
        var tomorrow = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("o"));

        // Act
        var all = await operatorClient.GetFromJsonAsync<JsonElement>($"{Api}/alarms/history?limit=1", Ct);
        var other = await operatorClient.GetFromJsonAsync<JsonElement>($"{Api}/alarms/history?asset=other", Ct);
        var later = await operatorClient.GetFromJsonAsync<JsonElement>($"{Api}/alarms/history?from={tomorrow}", Ct);

        // Assert
        all.GetProperty("total").GetInt32().ShouldBe(1);
        all.GetProperty("items").GetArrayLength().ShouldBe(1);
        other.GetProperty("total").GetInt32().ShouldBe(0);
        later.GetProperty("total").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task ListAssetAlarms_ShouldReturnNotFound_WhenTheAssetDoesNotExist()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (_, operatorClient) = await app.MemberAsync("Operator", "acme");

        // Act
        var response = await operatorClient.GetAsync($"{Api}/assets/ghost/alarms", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static PostgresAlarmHistory History(string connectionString) =>
        new(NpgsqlDataSource.Create(connectionString));

    private static AlarmCondition LowPressure(AlarmPriority priority = AlarmPriority.High) =>
        new("low", "pressure", "<", "3", null, null, priority, "Brake pressure below 3 bar");

    private static async Task<Guid> RaiseAsync(EngineApp app, HttpClient client)
    {
        await app.ApplyAsync("acme", "Asset", "train", Train, 1);
        var (_, writer) = await app.MemberAsync("Operator", "acme");
        var written = await Wait.UntilAsync(
            async () =>
            {
                var response = await writer.PutAsync(
                    $"{Api}/assets/train/datapoints/pressure",
                    new StringContent("""{"value": 2.5}""", Encoding.UTF8, "application/json"),
                    Ct);

                return response.StatusCode == HttpStatusCode.NoContent ? response : null;
            },
            Ct);
        written.EnsureSuccessStatusCode();
        var alarm = await Wait.UntilAsync(
            async () => (await ItemsAsync(client, $"{Api}/alarms")) is { Count: 1 } items
                ? items[0].Clone().ToString()
                : null,
            Ct);

        return JsonDocument.Parse(alarm).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<List<JsonElement>> ItemsAsync(HttpClient client, string route)
    {
        var body = await client.GetFromJsonAsync<JsonElement>(route, Ct);

        return [.. body.GetProperty("items").EnumerateArray().Select(item => item.Clone())];
    }
}
