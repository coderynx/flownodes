using System.Text.Json;
using Engine.Application.Devices;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class AssetDataPointTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private const string Simulator = """{"integration": "flownodes.simulator", "config": {"units": 2}}""";

    [Fact]
    public async Task Asset_ShouldReadTheDeviceValue_WhenADataPointIsMappedToIt()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await StartDevicesAsync(engine);

        // Act
        await engine.ApplyAsync("acme", "Asset", "room", Room("t1"), 5);

        // Assert
        var reading = await ReadAsync(engine, "room", "temperature", reading => reading.Value is not null);
        reading.Value!.Value.GetDouble().ShouldBe(20, 0.001);
        reading.Quality.ShouldBe("good");
        reading.Writable.ShouldBeFalse();
    }

    [Fact]
    public async Task Asset_ShouldKeepTheDataPointName_WhenTheMappingIsRepointedToAReplacementDevice()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await StartDevicesAsync(engine);
        await engine.Devices.InvokeActionAsync("acme", "t2", "set", Json("""{"value": 33}"""), Ct);
        await engine.ApplyAsync("acme", "Asset", "room", Room("t1"), 5);
        await ReadAsync(engine, "room", "temperature", reading => Near(reading, 20));

        // Act
        await engine.ApplyAsync("acme", "Asset", "room", Room("t2"), 6, 2);

        // Assert
        var reading = await ReadAsync(engine, "room", "temperature", reading => Near(reading, 33));
        reading.Name.ShouldBe("temperature");
        reading.Quality.ShouldBe("good");
    }

    [Fact]
    public async Task Asset_ShouldComputeFromOtherDataPoints_WhenADataPointIsComputed()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await StartDevicesAsync(engine);
        await engine.Devices.InvokeActionAsync("acme", "t2", "set", Json("""{"value": 30}"""), Ct);

        // Act
        await engine.ApplyAsync("acme", "Asset", "room", Pair(), 5);

        // Assert
        var reading = await ReadAsync(engine, "room", "average", reading => Near(reading, 25));
        reading.Quality.ShouldBe("good");
    }

    [Fact]
    public async Task Asset_ShouldBeBad_WhenAComputedDataPointDerivesFromABadValue()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await StartDevicesAsync(engine);
        await engine.ApplyAsync("acme", "Device", "orphan", Device("Thermometer", 9, "ghost"), 5);
        await engine.ApplyAsync("acme", "Asset", "room", Pair("orphan"), 6);

        // Act
        var reading = await ReadAsync(engine, "room", "average", reading => reading.Quality == "bad");

        // Assert
        reading.Quality.ShouldBe("bad");
    }

    [Fact]
    public async Task Asset_ShouldGoBad_WhenTheDeviceConnectionIsLost()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await StartDevicesAsync(engine);
        await engine.ApplyAsync("acme", "Asset", "room", Room("t1"), 5);
        await ReadAsync(engine, "room", "temperature", reading => reading.Quality == "good");

        // Act
        await engine.DeleteAsync("acme", "Connection", "sim", 6);

        // Assert
        var reading = await ReadAsync(engine, "room", "temperature", reading => reading.Quality == "bad");
        reading.Quality.ShouldBe("bad");
    }

    [Fact]
    public async Task WriteDataPointAsync_ShouldWriteThroughToTheDevice_WhenTheDataPointIsMappedToAWritableOne()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await StartDevicesAsync(engine);
        await engine.ApplyAsync(
            "acme",
            "Asset",
            "room",
            """{"dataPoints": [{"name": "power", "type": "bool", "writable": true, "source": {"device": "lamp", "dataPoint": "on"}}]}""",
            5);
        await ReadAsync(engine, "room", "power", reading => reading.Value is not null);

        // Act
        var result = await engine.Assets.WriteDataPointAsync("acme", "room", "power", Json("true"), Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        (await ReadAsync(engine, "room", "power", reading => reading.Value?.GetBoolean() == true)).Writable
            .ShouldBeTrue();
        var device = await engine.Devices.ListDataPointsAsync("acme", "lamp", Ct);
        device.Value.Single().Value!.Value.GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task WriteDataPointAsync_ShouldBeRefused_WhenTheDataPointIsComputedOrTheValueIsWrong()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await StartDevicesAsync(engine);
        await engine.ApplyAsync("acme", "Asset", "room", Pair(), 5);
        await ReadAsync(engine, "room", "average", reading => reading.Value is not null);

        // Act
        var computed = await engine.Assets.WriteDataPointAsync("acme", "room", "average", Json("1"), Ct);
        var missing = await engine.Assets.WriteDataPointAsync("acme", "room", "ghost", Json("1"), Ct);

        // Assert
        computed.Error.Code.ShouldBe("asset.ReadOnly");
        missing.Error.Code.ShouldBe("asset.not_found");
    }

    [Fact]
    public async Task WriteDataPointAsync_ShouldKeepTheValue_WhenAFlowWritesAnUnmappedDataPointAndTheAssetFailsOver()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync(
            "acme",
            "Asset",
            "room",
            """{"dataPoints": [{"name": "target", "type": "number", "min": 0, "max": 40}]}""",
            2);
        (await ReadAsync(engine, "room", "target", _ => true)).Quality.ShouldBe("uncertain");

        // Act
        var invalid = await engine.Assets.WriteDataPointAsync("acme", "room", "target", Json("99"), Ct);
        var written = await engine.Assets.WriteDataPointAsync("acme", "room", "target", Json("21.5"), Ct);
        await engine.DeactivateAllAsync();

        // Assert
        invalid.Error.Code.ShouldBe("asset.InvalidValue");
        written.IsSuccess.ShouldBeTrue();
        var reading = await ReadAsync(engine, "room", "target", reading => reading.Value is not null);
        reading.Value!.Value.GetDouble().ShouldBe(21.5, 0.001);
        reading.Quality.ShouldBe("good");
    }

    [Fact]
    public async Task Asset_ShouldReadTheMappingOfItsAssetType_WhenTheInstanceSuppliesTheSource()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await StartDevicesAsync(engine);
        await engine.ApplyAsync(
            "acme",
            "AssetType",
            "room",
            """{"dataPoints": [{"name": "temperature", "type": "number"}]}""",
            5);

        // Act
        await engine.ApplyAsync(
            "acme",
            "Asset",
            "room-1",
            """{"assetType": "room", "sources": {"temperature": {"device": "t1", "dataPoint": "temperature"}}}""",
            6);

        // Assert
        var reading = await ReadAsync(engine, "room-1", "temperature", reading => reading.Value is not null);
        reading.Value!.Value.GetDouble().ShouldBe(20, 0.001);
    }

    private static bool Near(DataPointReading reading, double expected) =>
        reading.Value is { } value && Math.Abs(value.GetDouble() - expected) < 0.001;

    private static async Task StartDevicesAsync(EngineCluster engine)
    {
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);
        await engine.ApplyAsync("acme", "Device", "t1", Device("Thermometer", 1, "sim"), 3);
        await engine.ApplyAsync("acme", "Device", "t2", Device("Thermometer", 2, "sim"), 4);
        await engine.ApplyAsync("acme", "Device", "lamp", Device("Switch", 1, "sim"), 5);
        foreach (var name in new[] { "t1", "t2", "lamp" })
        {
            await Eventually(async () =>
                (await engine.Devices.ListDataPointsAsync("acme", name, Ct)).Value.All(reading =>
                    reading is { Quality: "good", Value: not null }));
        }
    }

    private static async Task<DataPointReading> ReadAsync(
        EngineCluster engine,
        string asset,
        string dataPoint,
        Func<DataPointReading, bool> predicate)
    {
        DataPointReading? last = null;
        await Eventually(async () =>
        {
            var result = await engine.Assets.ListDataPointsAsync("acme", asset, Ct);
            last = result.IsSuccess ? result.Value.FirstOrDefault(reading => reading.Name == dataPoint) : null;

            return last is not null && predicate(last);
        });

        return last!;
    }

    private static async Task Eventually(Func<Task<bool>> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (!await condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("The condition did not hold in time.");
            }

            await Task.Delay(50, Ct);
        }
    }

    private static string Room(string device) =>
        $$$"""{"dataPoints": [{"name": "temperature", "type": "number", "source": {"device": "{{{device}}}", "dataPoint": "temperature"}}]}""";

    private static string Pair(string second = "t2") =>
        $$$$"""
            {"dataPoints": [
              {"name": "first", "type": "number", "source": {"device": "t1", "dataPoint": "temperature"}},
              {"name": "second", "type": "number", "source": {"device": "{{{{second}}}}", "dataPoint": "temperature"}},
              {"name": "average", "type": "number", "source": {"compute": {"op": "average", "inputs": ["first", "second"]}}}]}
            """;

    private static string Device(string type, int id, string connection) =>
        "{\"connection\": \"" + connection + "\", \"type\": \"" + type + "\", \"address\": {\"id\": " + id + "}}";

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
