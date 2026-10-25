using System.Text.Json;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class AssetTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private const string Train = """{"dataPoints": [{"name": "speed", "type": "number"}]}""";
    private const string Train1 = """{"assetType": "train", "dataPoints": [{"name": "doors", "type": "bool"}]}""";

    [Fact]
    public async Task Asset_ShouldMergeItsAssetTypeDataPoints_WhenItIsAnInstance()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "AssetType", "train", Train, 2);

        // Act
        await engine.ApplyAsync("acme", "Asset", "train-1", Train1, 3);

        // Assert
        var status = await engine.Status.WaitForAsync(
            "acme",
            "Asset",
            "train-1",
            status => RecordingStatus.Reason(status) == "Resolved",
            Ct);
        DataPoints(status).ShouldBe(["speed", "doors"]);
    }

    [Fact]
    public async Task Asset_ShouldReportAssetTypeMissing_WhenItsAssetTypeIsNotThere()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();

        // Act
        await engine.ApplyAsync("acme", "Asset", "train-1", Train1, 2);

        // Assert
        var status = await engine.Status.WaitForAsync(
            "acme",
            "Asset",
            "train-1",
            status => RecordingStatus.Reason(status) == "AssetTypeMissing",
            Ct);
        status.GetProperty("conditions")[0].GetProperty("status").GetString().ShouldBe("False");
    }

    [Fact]
    public async Task Asset_ShouldFollowItsAssetType_WhenTheAssetTypeChanges()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "AssetType", "train", Train, 2);
        await engine.ApplyAsync("acme", "Asset", "train-1", Train1, 3);
        await engine.Status.WaitForAsync(
            "acme",
            "Asset",
            "train-1",
            status => RecordingStatus.Reason(status) == "Resolved",
            Ct);

        // Act
        await engine.ApplyAsync(
            "acme",
            "AssetType",
            "train",
            """{"dataPoints": [{"name": "speed", "type": "number"}, {"name": "brake", "type": "bool"}]}""",
            4,
            2);

        // Assert
        var status = await engine.Status.WaitForAsync(
            "acme",
            "Asset",
            "train-1",
            status => DataPoints(status).Contains("brake"),
            Ct);
        DataPoints(status).ShouldBe(["speed", "brake", "doors"]);
    }

    [Fact]
    public async Task Asset_ShouldBecomeUnresolved_WhenItsAssetTypeIsDeleted()
    {
        // Arrange
        await using var engine = await EngineCluster.StartAsync();
        await engine.ApplyAsync("acme", "AssetType", "train", Train, 2);
        await engine.ApplyAsync("acme", "Asset", "train-1", Train1, 3);
        await engine.Status.WaitForAsync(
            "acme",
            "Asset",
            "train-1",
            status => RecordingStatus.Reason(status) == "Resolved",
            Ct);

        // Act
        await engine.DeleteAsync("acme", "AssetType", "train", 4);

        // Assert
        var status = await engine.Status.WaitForAsync(
            "acme",
            "Asset",
            "train-1",
            status => RecordingStatus.Reason(status) == "AssetTypeMissing",
            Ct);
        status.GetProperty("dataPoints").GetArrayLength().ShouldBe(0);
    }

    private static string[] DataPoints(JsonElement status) =>
        [.. status.GetProperty("dataPoints").EnumerateArray().Select(name => name.GetString()!)];
}
