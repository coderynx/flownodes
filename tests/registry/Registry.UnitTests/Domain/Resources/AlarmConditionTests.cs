using Registry.Application.Resources;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class AlarmConditionTests
{
    private static readonly NamespaceName Acme = NamespaceName.Create("acme");
    private const string Pressure = """{"name": "brakepressure", "type": "number"}""";

    private const string Low =
        """{"name": "low", "dataPoint": "brakepressure", "operator": "<", "value": 3, "for": "10s", "deadband": 0.2, "priority": "high", "message": "Brake pressure below 3 bar"}""";

    [Fact]
    public void AssetTypeValidate_ShouldAcceptTheSpec_WhenItDeclaresValidAlarmConditions()
    {
        // Arrange
        var spec = Widgets.Json($$"""{"dataPoints": [{{Pressure}}], "alarms": [{{Low}}]}""");

        // Act
        var errors = new AssetTypeKind().Validate(spec, Snapshot());

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"name": "m", "type": "enum", "values": ["a", "b"]}""", "==", "\"a\"")]
    [InlineData("""{"name": "m", "type": "enum", "values": ["a", "b"]}""", "!=", "\"b\"")]
    [InlineData("""{"name": "m", "type": "bool"}""", "==", "true")]
    [InlineData("""{"name": "m", "type": "string"}""", "!=", "\"x\"")]
    [InlineData("""{"name": "m", "type": "integer"}""", ">=", "4")]
    [InlineData("""{"name": "m", "type": "number"}""", "<=", "4.5")]
    public void AssetTypeValidate_ShouldAcceptTheValue_WhenItMatchesTheDataPointType(
        string dataPoint,
        string op,
        string value)
    {
        // Arrange
        var spec = Widgets.Json(
            $$"""{"dataPoints": [{{dataPoint}}], "alarms": [{"name": "a", "dataPoint": "m", "operator": "{{op}}", "value": {{value}}, "priority": "low", "message": "m"}]}""");

        // Act
        var errors = new AssetTypeKind().Validate(spec, Snapshot());

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"alarms": {}}""", "spec.alarms")]
    [InlineData("""{"alarms": [1]}""", "spec.alarms[0]")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "ghost", "operator": "<", "value": 1, "priority": "low", "message": "m"}]}""",
        "spec.alarms[0].dataPoint")]
    [InlineData(
        """{"alarms": [{"dataPoint": "p", "operator": "<", "value": 1, "priority": "low", "message": "m"}]}""",
        "spec.alarms[0].name")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "~", "value": 1, "priority": "low", "message": "m"}]}""",
        "spec.alarms[0].operator")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "value": "x", "priority": "low", "message": "m"}]}""",
        "spec.alarms[0].value")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "priority": "low", "message": "m"}]}""",
        "spec.alarms[0].value")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "value": 1, "for": "0s", "priority": "low", "message": "m"}]}""",
        "spec.alarms[0].for")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "value": 1, "for": "soon", "priority": "low", "message": "m"}]}""",
        "spec.alarms[0].for")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "value": 1, "deadband": -1, "priority": "low", "message": "m"}]}""",
        "spec.alarms[0].deadband")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "==", "value": 1, "deadband": 1, "priority": "low", "message": "m"}]}""",
        "spec.alarms[0].deadband")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "value": 1, "message": "m"}]}""",
        "spec.alarms[0].priority")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "value": 1, "priority": "urgent", "message": "m"}]}""",
        "spec.alarms[0].priority")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "value": 1, "priority": "low"}]}""",
        "spec.alarms[0].message")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "value": 1, "priority": "low", "message": "m", "typo": 1}]}""",
        "spec.alarms[0].typo")]
    [InlineData(
        """{"alarms": [{"name": "a", "dataPoint": "p", "operator": "<", "value": 1, "priority": "low", "message": "m"}, {"name": "a", "dataPoint": "p", "operator": ">", "value": 1, "priority": "low", "message": "m"}]}""",
        "spec.alarms[1].name")]
    public void AssetTypeValidate_ShouldReportThePath_WhenAnAlarmConditionIsInvalid(string alarms, string path)
    {
        // Arrange
        var spec = Widgets.Json(
            alarms.Replace("{\"alarms\":", "{\"dataPoints\": [{\"name\": \"p\", \"type\": \"number\"}], \"alarms\":"));

        // Act
        var errors = new AssetTypeKind().Validate(spec, Snapshot()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    [Theory]
    [InlineData("""{"name": "m", "type": "bool"}""", "<", "true", "spec.alarms[0].operator")]
    [InlineData("""{"name": "m", "type": "string"}""", ">=", "\"x\"", "spec.alarms[0].operator")]
    [InlineData("""{"name": "m", "type": "enum", "values": ["a"]}""", ">", "\"a\"", "spec.alarms[0].operator")]
    [InlineData("""{"name": "m", "type": "enum", "values": ["a"]}""", "==", "\"z\"", "spec.alarms[0].value")]
    [InlineData("""{"name": "m", "type": "bool"}""", "==", "1", "spec.alarms[0].value")]
    [InlineData("""{"name": "m", "type": "integer"}""", ">", "2.5", "spec.alarms[0].value")]
    [InlineData("""{"name": "m", "type": "string"}""", "==", "3", "spec.alarms[0].value")]
    public void AssetTypeValidate_ShouldReportTheMismatch_WhenTheOperatorOrValueDoesNotFitTheDataPointType(
        string dataPoint,
        string op,
        string value,
        string path)
    {
        // Arrange
        var spec = Widgets.Json(
            $$"""{"dataPoints": [{{dataPoint}}], "alarms": [{"name": "a", "dataPoint": "m", "operator": "{{op}}", "value": {{value}}, "priority": "low", "message": "m"}]}""");

        // Act
        var errors = new AssetTypeKind().Validate(spec, Snapshot()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    [Fact]
    public void AssetValidate_ShouldAcceptTheSpec_WhenItAddsAlarmConditionsOnInheritedDataPoints()
    {
        // Arrange
        var spec = Widgets.Json(
            """{"assetType": "train", "alarms": [{"name": "high", "dataPoint": "brakepressure", "operator": ">", "value": 9, "priority": "low", "message": "m"}]}""");
        var snapshot = Snapshot(
            Resource("AssetType", "train", $$"""{"dataPoints": [{{Pressure}}], "alarms": [{{Low}}]}"""));

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, snapshot);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void AssetValidate_ShouldReportTheClash_WhenItRedefinesAnInheritedAlarmCondition()
    {
        // Arrange
        var spec = Widgets.Json(
            """{"assetType": "train", "alarms": [{"name": "low", "dataPoint": "brakepressure", "operator": ">", "value": 9, "priority": "low", "message": "m"}]}""");
        var snapshot = Snapshot(
            Resource("AssetType", "train", $$"""{"dataPoints": [{{Pressure}}], "alarms": [{{Low}}]}"""));

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, snapshot).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == "spec.alarms[0].name");
    }

    [Fact]
    public void AssetValidate_ShouldReportTheDataPoint_WhenNeitherTheAssetNorItsAssetTypeDeclaresIt()
    {
        // Arrange
        var spec = Widgets.Json(
            """{"assetType": "train", "alarms": [{"name": "x", "dataPoint": "ghost", "operator": ">", "value": 9, "priority": "low", "message": "m"}]}""");
        var snapshot = Snapshot(Resource("AssetType", "train", $$"""{"dataPoints": [{{Pressure}}]}"""));

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, snapshot).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == "spec.alarms[0].dataPoint");
    }

    [Fact]
    public void AssetValidate_ShouldAcceptAlarmConditions_WhenTheAssetIsAdHoc()
    {
        // Arrange
        var spec = Widgets.Json($$"""{"dataPoints": [{{Pressure}}], "alarms": [{{Low}}]}""");

        // Act
        var errors = new AssetKind(LampCatalog.Sources()).Validate(spec, Snapshot());

        // Assert
        errors.ShouldBeEmpty();
    }

    private static SnapshotResource Resource(string kind, string name, string spec) =>
        new(kind, ResourceName.Create(name), Widgets.Json(spec));

    private static NamespaceSnapshot Snapshot(params SnapshotResource[] resources) => new(Acme, resources);
}
