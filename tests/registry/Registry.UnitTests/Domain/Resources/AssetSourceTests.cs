using Registry.Application.Resources;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class AssetSourceTests
{
    private static readonly NamespaceName Acme = NamespaceName.Create("acme");

    [Fact]
    public void AssetValidate_ShouldAcceptTheSpec_WhenADataPointMapsToAWritableDeviceDataPoint()
    {
        // Arrange
        var spec = Widgets.Json(
            """
            {"dataPoints": [{"name": "light", "type": "number", "writable": true,
              "source": {"device": "bulb-1", "dataPoint": "brightness"}}]}
            """);

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, Snapshot(Hub(), Bulb("bulb-1")));

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(
        """{"device": "bulb-1", "dataPoint": "ghost"}""",
        "number",
        false,
        "spec.dataPoints[0].source.dataPoint")]
    [InlineData(
        """{"device": "bulb-1", "dataPoint": "brightness"}""",
        "bool",
        false,
        "spec.dataPoints[0].source.dataPoint")]
    [InlineData(
        """{"device": "bulb-1", "dataPoint": "level"}""",
        "number",
        true,
        "spec.dataPoints[0].source.dataPoint")]
    [InlineData("""{"device": "bulb-1"}""", "number", false, "spec.dataPoints[0].source.dataPoint")]
    [InlineData("""{"dataPoint": "level"}""", "number", false, "spec.dataPoints[0].source.device")]
    [InlineData(
        """{"device": "bulb-1", "dataPoint": "level", "compute": {}}""",
        "number",
        false,
        "spec.dataPoints[0].source")]
    [InlineData("""{"typo": 1}""", "number", false, "spec.dataPoints[0].source.typo")]
    public void AssetValidate_ShouldReportThePath_WhenTheDeviceMappingIsInvalid(
        string source,
        string type,
        bool writable,
        string path)
    {
        // Arrange
        var spec = Widgets.Json(
            $$"""{"dataPoints": [{"name": "light", "type": "{{type}}", "writable": {{writable.ToString().ToLowerInvariant()}}, "source": {{source}}}]}""");

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, Snapshot(Hub(), Bulb("bulb-1"))).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    [Fact]
    public void AssetValidate_ShouldAcceptTheSpec_WhenADataPointIsComputedFromOthers()
    {
        // Arrange
        var spec = Widgets.Json(
            """
            {"dataPoints": [
              {"name": "a", "type": "number"}, {"name": "b", "type": "integer"},
              {"name": "total", "type": "number", "source": {"compute": {"op": "sum", "inputs": ["a", "b"]}}}]}
            """);

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, Snapshot());

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"op": "median", "inputs": ["a"]}""", "spec.dataPoints[1].source.compute.op")]
    [InlineData("""{"op": "sum", "inputs": []}""", "spec.dataPoints[1].source.compute.inputs")]
    [InlineData("""{"op": "sum", "inputs": ["ghost"]}""", "spec.dataPoints[1].source.compute.inputs[0]")]
    [InlineData("""{"op": "sum", "inputs": ["total"]}""", "spec.dataPoints[1].source.compute.inputs[0]")]
    [InlineData("""{"op": "sum", "inputs": ["flag"]}""", "spec.dataPoints[1].source.compute.inputs[0]")]
    public void AssetValidate_ShouldReportThePath_WhenTheComputationIsInvalid(string compute, string path)
    {
        // Arrange
        var spec = Widgets.Json(
            $$$"""
               {"dataPoints": [
                 {"name": "a", "type": "number"},
                 {"name": "total", "type": "number", "source": {"compute": {{{compute}}}}},
                 {"name": "flag", "type": "bool"}]}
               """);

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, Snapshot()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path.Replace("dataPoints[1]", "dataPoints[1]"));
    }

    [Fact]
    public void AssetValidate_ShouldReportACycle_WhenComputedDataPointsDependOnEachOther()
    {
        // Arrange
        var spec = Widgets.Json(
            """
            {"dataPoints": [
              {"name": "a", "type": "number", "source": {"compute": {"op": "sum", "inputs": ["b"]}}},
              {"name": "b", "type": "number", "source": {"compute": {"op": "sum", "inputs": ["a"]}}}]}
            """);

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, Snapshot()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == "spec.dataPoints[0].source");
        errors.ShouldContain(error => error.Path == "spec.dataPoints[1].source");
    }

    [Fact]
    public void AssetValidate_ShouldAcceptTheSpec_WhenItMapsADataPointOfItsAssetType()
    {
        // Arrange
        var spec = Widgets.Json(
            """{"assetType": "room", "sources": {"light": {"device": "bulb-1", "dataPoint": "brightness"}}}""");
        var room = new SnapshotResource(
            "AssetType",
            ResourceName.Create("room"),
            Widgets.Json("""{"dataPoints": [{"name": "light", "type": "number", "writable": true}]}"""));

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, Snapshot(Hub(), Bulb("bulb-1"), room));

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void AssetValidate_ShouldReportThePath_WhenItMapsADataPointItsAssetTypeDoesNotDeclare()
    {
        // Arrange
        var spec = Widgets.Json("""{"sources": {"light": {"device": "bulb-1", "dataPoint": "brightness"}}}""");

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, Snapshot(Hub(), Bulb("bulb-1"))).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == "spec.sources.light");
    }

    [Fact]
    public void AssetTypeValidate_ShouldReportThePath_WhenADataPointMapsFromADevice()
    {
        // Arrange
        var spec = Widgets.Json(
            """
            {"dataPoints": [{"name": "light", "type": "number",
              "source": {"device": "bulb-1", "dataPoint": "brightness"}}]}
            """);

        // Act
        var errors = new AssetTypeKind().Validate(spec, Snapshot()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == "spec.dataPoints[0].source");
    }

    [Fact]
    public void AssetReferences_ShouldNameTheDevices_WhenDataPointsMapFromThem()
    {
        // Arrange
        var spec = Widgets.Json(
            """
            {"dataPoints": [{"name": "light", "type": "number",
              "source": {"device": "bulb-1", "dataPoint": "brightness"}}],
             "sources": {"dimmer": {"device": "bulb-2", "dataPoint": "level"}}}
            """);

        // Act
        var references = new AssetKind(LampCatalog.Sources()).References(spec).ToList();

        // Assert
        references.ShouldBe(
        [
            new SpecReference("Device", "bulb-1", "spec.dataPoints[0].source.device"),
            new SpecReference("Device", "bulb-2", "spec.sources.dimmer.device")
        ]);
    }

    private static SnapshotResource Hub() =>
        new(
            "Connection",
            ResourceName.Create("hub"),
            Widgets.Json("""{"integration": "acme.lamp", "config": {"host": "x"}}"""));

    private static SnapshotResource Bulb(string name) =>
        new(
            "Device",
            ResourceName.Create(name),
            Widgets.Json("""{"connection": "hub", "type": "Bulb", "address": {"id": 3}}"""));

    private static NamespaceSnapshot Snapshot(params SnapshotResource[] resources) => new(Acme, resources);
}
