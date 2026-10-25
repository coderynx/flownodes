using System.Text.Json;
using Engine.Application.Devices;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class ConnectionAndDeviceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private const string Simulator = """{"integration": "flownodes.simulator", "config": {"units": 2}}""";

    [Fact]
    public async Task Connection_ShouldReportConnectedWithDiscoveries_WhenItIsApplied()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();

        // Act
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);

        // Assert
        var status = await engine.Status.WaitForAsync(
            "acme",
            "Connection",
            "sim",
            status => RecordingStatus.Reason(status) == "Connected" &&
                      status.GetProperty("discoveries").GetArrayLength() == 2,
            Ct);
        status.GetProperty("connectivity").GetString().ShouldBe("Connected");
        status.GetProperty("discoveries")
            .EnumerateArray()
            .Select(found => found.GetProperty("suggestedName").GetString())
            .ShouldBe(["thermometer-1", "thermometer-2"]);
    }

    [Fact]
    public async Task Connection_ShouldReportIntegrationMissing_WhenTheIntegrationIsNotInstalled()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();

        // Act
        await engine.ApplyAsync("acme", "Connection", "ghost", """{"integration": "acme.ghost"}""", 2);

        // Assert
        var status = await engine.Status.WaitForAsync(
            "acme",
            "Connection",
            "ghost",
            status => RecordingStatus.Reason(status) == "IntegrationMissing",
            Ct);
        status.GetProperty("conditions")[0].GetProperty("status").GetString().ShouldBe("False");
    }

    [Fact]
    public async Task Device_ShouldExposeDataPointWithGoodQuality_WhenItsConnectionFindsIt()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);

        // Act
        await engine.ApplyAsync("acme", "Device", "t1", Thermometer(1), 3);

        // Assert
        var reading = await TemperatureAsync(engine, "t1", reading => reading.Value is not null);
        reading.Quality.ShouldBe("good");
        reading.Value!.Value.GetDouble().ShouldBe(20, 0.001);
        reading.Unit.ShouldBe("°C");
        reading.SourceTimestamp.ShouldNotBeNull();
        (await engine.Status.WaitForAsync(
                "acme",
                "Device",
                "t1",
                status => RecordingStatus.Reason(status) == "Found",
                Ct)).GetProperty("state")
            .GetString()
            .ShouldBe("Found");
    }

    [Fact]
    public async Task Device_ShouldBeNotFound_WhenItWasDeclaredBeforeItWasDiscovered()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);

        // Act
        await engine.ApplyAsync("acme", "Device", "train", Thermometer(9), 3);

        // Assert
        await engine.Status.WaitForAsync(
            "acme",
            "Device",
            "train",
            status => RecordingStatus.Reason(status) == "NotFound",
            Ct);
        (await TemperatureAsync(engine, "train", _ => true)).Quality.ShouldBe("bad");
    }

    [Fact]
    public async Task Connection_ShouldNotListDiscoveriesAlreadyDeclared_WhenADeviceIsDeclared()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);

        // Act
        await engine.ApplyAsync("acme", "Device", "t1", Thermometer(1), 3);

        // Assert
        var status = await engine.Status.WaitForAsync(
            "acme",
            "Connection",
            "sim",
            status => status.GetProperty("discoveries").GetArrayLength() == 1,
            Ct);
        status.GetProperty("discoveries")[0].GetProperty("suggestedName").GetString().ShouldBe("thermometer-2");
    }

    [Fact]
    public async Task Device_ShouldTurnBadThenRecover_WhenItsConnectionIsLostAndComesBack()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);
        await engine.ApplyAsync("acme", "Device", "t1", Thermometer(1), 3);
        await TemperatureAsync(engine, "t1", reading => reading.Quality == "good" && reading.Value is not null);

        // Act
        await engine.Devices.InvokeActionAsync("acme", "t1", "fail", Json("{}"), Ct);

        // Assert
        await engine.Status.WaitForAsync(
            "acme",
            "Connection",
            "sim",
            status => RecordingStatus.Reason(status) == "Failed",
            Ct);
        (await TemperatureAsync(engine, "t1", _ => true)).Quality.ShouldBe("bad");
        await engine.Status.WaitForAsync(
            "acme",
            "Connection",
            "sim",
            status => RecordingStatus.Reason(status) == "Connected",
            Ct);
        (await TemperatureAsync(engine, "t1", reading => reading.Quality == "good")).Value!.Value.GetDouble()
            .ShouldBe(20, 0.001);
    }

    [Fact]
    public async Task WriteDataPointAsync_ShouldReflectTheConfirmedValue_WhenTheDataPointIsWritable()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);
        await engine.ApplyAsync("acme", "Device", "lamp", Switch(1), 3);
        await Single(engine, "lamp", "on", r => r.Value is not null);

        // Act
        var result = await engine.Devices.WriteDataPointAsync("acme", "lamp", "on", Json("true"), Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var reading = await Single(engine, "lamp", "on", r => r.Value?.GetBoolean() == true);
        reading.Quality.ShouldBe("good");
    }

    [Fact]
    public async Task WriteDataPointAsync_ShouldBeRefused_WhenTheDataPointIsReadOnly()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);
        await engine.ApplyAsync("acme", "Device", "t1", Thermometer(1), 3);
        await TemperatureAsync(engine, "t1", reading => reading.Value is not null);

        // Act
        var result = await engine.Devices.WriteDataPointAsync("acme", "t1", "temperature", Json("30"), Ct);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("device.ReadOnly");
    }

    [Fact]
    public async Task WriteDataPointAsync_ShouldBeRefused_WhenTheValueHasTheWrongType()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);
        await engine.ApplyAsync("acme", "Device", "lamp", Switch(1), 3);
        await Single(engine, "lamp", "on", r => r.Value is not null);

        // Act
        var result = await engine.Devices.WriteDataPointAsync("acme", "lamp", "on", Json("\"yes\""), Ct);

        // Assert
        result.Error.Code.ShouldBe("device.InvalidValue");
    }

    [Fact]
    public async Task InvokeActionAsync_ShouldReturnTheDevicesResult_WhenTheActionExists()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);
        await engine.ApplyAsync("acme", "Device", "t1", Thermometer(1), 3);
        await TemperatureAsync(engine, "t1", reading => reading.Value is not null);

        // Act
        var result = await engine.Devices.InvokeActionAsync("acme", "t1", "set", Json("""{"value": 42.5}"""), Ct);

        // Assert
        result.Value.GetProperty("temperature").GetDouble().ShouldBe(42.5, 0.001);
        (await TemperatureAsync(engine, "t1", reading => Math.Abs((reading.Value?.GetDouble() ?? 0) - 42.5) < 0.001))
            .Quality.ShouldBe("good");
    }

    [Fact]
    public async Task InvokeActionAsync_ShouldBeNotFound_WhenTheDeviceHasNoSuchAction()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);
        await engine.ApplyAsync("acme", "Device", "t1", Thermometer(1), 3);
        await TemperatureAsync(engine, "t1", reading => reading.Value is not null);

        // Act
        var result = await engine.Devices.InvokeActionAsync("acme", "t1", "explode", Json("{}"), Ct);

        // Assert
        result.Error.Code.ShouldBe("device.not_found");
    }

    [Fact]
    public async Task Connection_ShouldStopAndItsDevicesGoBad_WhenItIsDeleted()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);
        await engine.ApplyAsync("acme", "Device", "t1", Thermometer(1), 3);
        await TemperatureAsync(engine, "t1", reading => reading.Quality == "good" && reading.Value is not null);

        // Act
        await engine.DeleteAsync("acme", "Connection", "sim", 4);

        // Assert
        await engine.Status.WaitForAsync(
            "acme",
            "Device",
            "t1",
            status => RecordingStatus.Reason(status) == "ConnectionNotReady",
            Ct);
        (await TemperatureAsync(engine, "t1", _ => true)).Quality.ShouldBe("bad");
    }

    private static string Thermometer(int id) =>
        Device("Thermometer", id);

    private static string Switch(int id) =>
        Device("Switch", id);

    private static string Device(string type, int id) =>
        "{\"connection\": \"sim\", \"type\": \"" + type + "\", \"address\": {\"id\": " + id + "}}";

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static async Task<IReadOnlyList<DataPointReading>> Readings(EngineCluster engine, string device)
    {
        var result = await engine.Devices.ListDataPointsAsync("acme", device, Ct);

        return result.IsSuccess ? result.Value : [];
    }

    private static Task<DataPointReading> Single(
        EngineCluster engine,
        string device,
        string dataPoint,
        Func<DataPointReading, bool> condition) =>
        Wait.UntilAsync(
            async () => (await Readings(engine, device)).FirstOrDefault(r => r.Name == dataPoint && condition(r)),
            Ct);

    private static Task<DataPointReading> TemperatureAsync(
        EngineCluster engine,
        string device,
        Func<DataPointReading, bool> condition) =>
        Single(engine, device, "temperature", condition);
}
