using Registry.Application.Resources;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class AssetKindTests
{
    private static readonly NamespaceName Acme = NamespaceName.Create("acme");

    [Fact]
    public void AssetTypeValidate_ShouldAcceptTheSpec_WhenItDeclaresDataPoints()
    {
        // Arrange
        var spec = Widgets.Json(
            """
            {"dataPoints": [
              {"name": "speed", "type": "number", "unit": "km/h", "min": 0, "max": 300},
              {"name": "mode", "type": "enum", "values": ["idle", "run"], "writable": true}]}
            """);

        // Act
        var errors = new AssetTypeKind().Validate(spec, Snapshot());

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"extra": 1}""", "spec.extra")]
    [InlineData("""{"dataPoints": {}}""", "spec.dataPoints")]
    [InlineData("""{"dataPoints": [{"type": "bool"}]}""", "spec.dataPoints[0].name")]
    [InlineData("""{"dataPoints": [{"name": "Bad Name", "type": "bool"}]}""", "spec.dataPoints[0].name")]
    [InlineData("""{"dataPoints": [{"name": "a"}]}""", "spec.dataPoints[0].type")]
    [InlineData("""{"dataPoints": [{"name": "a", "type": "blob"}]}""", "spec.dataPoints[0].type")]
    [InlineData("""{"dataPoints": [{"name": "a", "type": "enum"}]}""", "spec.dataPoints[0].values")]
    [InlineData("""{"dataPoints": [{"name": "a", "type": "bool", "min": 1}]}""", "spec.dataPoints[0].min")]
    [InlineData("""{"dataPoints": [{"name": "a", "type": "number", "min": 5, "max": 1}]}""", "spec.dataPoints[0].max")]
    [InlineData("""{"dataPoints": [{"name": "a", "type": "bool", "typo": 1}]}""", "spec.dataPoints[0].typo")]
    [InlineData(
        """{"dataPoints": [{"name": "a", "type": "bool"}, {"name": "a", "type": "bool"}]}""",
        "spec.dataPoints[1].name")]
    public void AssetTypeValidate_ShouldReportThePath_WhenTheSpecIsInvalid(string json, string path)
    {
        // Act
        var errors = new AssetTypeKind().Validate(Widgets.Json(json), Snapshot()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    [Fact]
    public void AssetValidate_ShouldAcceptTheSpec_WhenItExtendsItsAssetType()
    {
        // Arrange
        var spec = Widgets.Json(
            """{"assetType": "train", "parent": "fleet", "dataPoints": [{"name": "doors", "type": "bool"}]}""");
        var snapshot = Snapshot(
            Resource("AssetType", "train", """{"dataPoints": [{"name": "speed", "type": "number"}]}"""),
            Resource("Asset", "fleet", "{}"));

        // Act
        var errors = new AssetKind().Validate(spec, snapshot);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void AssetValidate_ShouldReportTheClash_WhenItRedefinesADataPointOfItsAssetType()
    {
        // Arrange
        var spec = Widgets.Json("""{"assetType": "train", "dataPoints": [{"name": "speed", "type": "integer"}]}""");
        var snapshot = Snapshot(
            Resource("AssetType", "train", """{"dataPoints": [{"name": "speed", "type": "number"}]}"""));

        // Act
        var errors = new AssetKind().Validate(spec, snapshot).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == "spec.dataPoints[0].name");
    }

    [Fact]
    public void AssetValidate_ShouldAcceptTheSpec_WhenItsAssetTypeDoesNotExist()
    {
        // Arrange
        var spec = Widgets.Json("""{"assetType": "ghost"}""");

        // Act
        var errors = new AssetKind().Validate(spec, Snapshot());

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"extra": 1}""", "spec.extra")]
    [InlineData("""{"assetType": 3}""", "spec.assetType")]
    [InlineData("""{"parent": 3}""", "spec.parent")]
    [InlineData("""{"dataPoints": [{"name": "a"}]}""", "spec.dataPoints[0].type")]
    public void AssetValidate_ShouldReportThePath_WhenTheSpecIsInvalid(string json, string path)
    {
        // Act
        var errors = new AssetKind().Validate(Widgets.Json(json), Snapshot()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    [Fact]
    public void AssetValidate_ShouldReportACycle_WhenTheParentsLoopBack()
    {
        // Arrange
        var spec = Widgets.Json("""{"parent": "b"}""");
        var snapshot = Snapshot(
            Resource("Asset", "a", """{"parent": "b"}"""),
            Resource("Asset", "b", """{"parent": "a"}"""));

        // Act
        var errors = new AssetKind().Validate(spec, snapshot).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == "spec.parent");
    }

    [Fact]
    public void AssetValidate_ShouldAcceptTheSpec_WhenTheParentsFormAChain()
    {
        // Arrange
        var spec = Widgets.Json("""{"parent": "b"}""");
        var snapshot = Snapshot(Resource("Asset", "b", """{"parent": "c"}"""), Resource("Asset", "c", "{}"));

        // Act
        var errors = new AssetKind().Validate(spec, snapshot);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void AssetReferences_ShouldNameTheAssetTypeAndTheParent_WhenTheSpecHasThem()
    {
        // Arrange
        var spec = Widgets.Json("""{"assetType": "train", "parent": "fleet"}""");

        // Act
        var references = new AssetKind().References(spec).ToList();

        // Assert
        references.ShouldBe(
        [
            new SpecReference("AssetType", "train", "spec.assetType"),
            new SpecReference("Asset", "fleet", "spec.parent")
        ]);
    }

    private static SnapshotResource Resource(string kind, string name, string spec) =>
        new(kind, ResourceName.Create(name), Widgets.Json(spec));

    private static NamespaceSnapshot Snapshot(params SnapshotResource[] resources) => new(Acme, resources);
}
