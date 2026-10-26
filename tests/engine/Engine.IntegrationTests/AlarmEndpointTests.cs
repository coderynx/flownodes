using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class AlarmEndpointTests(PostgresFixture postgres)
{
    private const string Api = "/api/v1/engine/namespaces/acme";
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

    [Theory]
    [InlineData("Operator")]
    [InlineData("Engineer")]
    public async Task AcknowledgeAlarm_ShouldRecordWhoAndWhen_WhenTheCallerIsAnOperatorOrEngineer(string role)
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(await postgres.CreateDatabaseAsync());
        var (userId, member) = await app.MemberAsync(role, "acme");
        var id = await RaiseAsync(app, member);

        // Act
        var response = await member.PostAsJsonAsync($"{Api}/alarms/{id}/acknowledge", new { comment = "on it" }, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var live = await ItemsAsync(member, $"{Api}/assets/train/alarms");
        live.Single().GetProperty("acknowledgedBy").GetGuid().ShouldBe(userId);
        live.Single().GetProperty("acknowledgementComment").GetString().ShouldBe("on it");
        live.Single().GetProperty("state").GetString().ShouldBe("active");
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
