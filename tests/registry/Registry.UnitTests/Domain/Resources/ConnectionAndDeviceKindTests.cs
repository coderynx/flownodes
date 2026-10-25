using System.Text.Json;
using Core.Application.Integrations;
using Registry.Application.Resources;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class ConnectionAndDeviceKindTests
{
    private static readonly NamespaceName Acme = NamespaceName.Create("acme");

    [Fact]
    public void ConnectionValidate_ShouldAcceptTheSpec_WhenTheIntegrationIsInstalledAndConfigIsValid()
    {
        // Arrange
        var kind = new ConnectionKind(new Catalog());
        var spec = Widgets.Json("""{"integration": "acme.lamp", "config": {"host": "10.0.0.1"}}""");

        // Act
        var errors = kind.Validate(spec, Snapshot());

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"config": {}}""", "spec.integration")]
    [InlineData("""{"integration": "acme.missing"}""", "spec.integration")]
    [InlineData("""{"integration": "acme.lamp"}""", "spec.config.host")]
    [InlineData("""{"integration": "acme.lamp", "config": {"host": 1}}""", "spec.config.host")]
    [InlineData("""{"integration": "acme.lamp", "config": {"host": "x", "typo": 1}}""", "spec.config.typo")]
    [InlineData("""{"integration": "acme.lamp", "config": {"host": "x"}, "extra": 1}""", "spec.extra")]
    public void ConnectionValidate_ShouldReportThePath_WhenTheSpecIsInvalid(string json, string path)
    {
        // Arrange
        var kind = new ConnectionKind(new Catalog());

        // Act
        var errors = kind.Validate(Widgets.Json(json), Snapshot()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    [Fact]
    public void DeviceValidate_ShouldAcceptTheSpec_WhenTheAddressFitsTheDeviceType()
    {
        // Arrange
        var kind = new DeviceKind(new Catalog());
        var spec = Widgets.Json("""{"connection": "hub", "type": "Bulb", "address": {"id": 3}}""");

        // Act
        var errors = kind.Validate(spec, Snapshot(Hub()));

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"type": "Bulb", "address": {"id": 3}}""", "spec.connection")]
    [InlineData("""{"connection": "hub", "address": {"id": 3}}""", "spec.type")]
    [InlineData("""{"connection": "hub", "type": "Bulb"}""", "spec.address")]
    [InlineData("""{"connection": "hub", "type": "Toaster", "address": {"id": 3}}""", "spec.type")]
    [InlineData("""{"connection": "hub", "type": "Bulb", "address": {"id": "x"}}""", "spec.address.id")]
    [InlineData("""{"connection": "hub", "type": "Bulb", "address": {}}""", "spec.address.id")]
    [InlineData("""{"connection": "hub", "type": "Bulb", "address": {"id": 3}, "config": {"x": 1}}""", "spec.config.x")]
    public void DeviceValidate_ShouldReportThePath_WhenTheSpecIsInvalid(string json, string path)
    {
        // Arrange
        var kind = new DeviceKind(new Catalog());

        // Act
        var errors = kind.Validate(Widgets.Json(json), Snapshot(Hub())).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    [Fact]
    public void DeviceReferences_ShouldNameTheConnection_WhenTheSpecHasOne()
    {
        // Arrange
        var kind = new DeviceKind(new Catalog());
        var spec = Widgets.Json("""{"connection": "hub", "type": "Bulb", "address": {"id": 3}}""");

        // Act
        var references = kind.References(spec).ToList();

        // Assert
        references.ShouldBe([new SpecReference("Connection", "hub", "spec.connection")]);
    }

    [Fact]
    public void DeviceReferences_ShouldNameTheAsset_WhenTheDeviceIsAttached()
    {
        // Arrange
        var kind = new DeviceKind(new Catalog());
        var spec = Widgets.Json("""{"connection": "hub", "type": "Bulb", "address": {"id": 3}, "asset": "train-1"}""");

        // Act
        var references = kind.References(spec).ToList();

        // Assert
        references.ShouldBe(
        [
            new SpecReference("Connection", "hub", "spec.connection"),
            new SpecReference("Asset", "train-1", "spec.asset")
        ]);
    }

    [Fact]
    public void DeviceValidate_ShouldAcceptTheSpec_WhenItIsAttachedToAnAsset()
    {
        // Arrange
        var kind = new DeviceKind(new Catalog());
        var spec = Widgets.Json("""{"connection": "hub", "type": "Bulb", "address": {"id": 3}, "asset": "train-1"}""");

        // Act
        var errors = kind.Validate(spec, Snapshot(Hub()));

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void DeviceValidate_ShouldReportThePath_WhenTheAssetIsNotAName()
    {
        // Arrange
        var kind = new DeviceKind(new Catalog());
        var spec = Widgets.Json("""{"connection": "hub", "type": "Bulb", "address": {"id": 3}, "asset": 3}""");

        // Act
        var errors = kind.Validate(spec, Snapshot(Hub())).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == "spec.asset");
    }

    private static SnapshotResource Hub() =>
        new(
            "Connection",
            ResourceName.Create("hub"),
            Widgets.Json("""{"integration": "acme.lamp", "config": {"host": "x"}}"""));

    private static NamespaceSnapshot Snapshot(params SnapshotResource[] resources) => new(Acme, resources);

    private sealed class Catalog : IIntegrationSchemas
    {
        public IntegrationSchema? Find(string id) =>
            id == "acme.lamp"
                ? new IntegrationSchema(
                    config => Check(config, "host", JsonValueKind.String),
                    [
                        new DeviceTypeSchema(
                            "Bulb",
                            address => Check(address, "id", JsonValueKind.Number),
                            config => Check(config, null, JsonValueKind.Undefined))
                    ])
                : null;

        private static List<SchemaProblem> Check(JsonElement value, string? required, JsonValueKind kind)
        {
            var problems = value.EnumerateObject()
                .Where(property => property.Name != required)
                .Select(property => new SchemaProblem(property.Name, "Unknown field."))
                .ToList();
            if (required is null)
            {
                return problems;
            }

            if (!value.TryGetProperty(required, out var found))
            {
                problems.Add(new SchemaProblem(required, "Required."));
            }
            else if (found.ValueKind != kind)
            {
                problems.Add(new SchemaProblem(required, "Wrong type."));
            }

            return problems;
        }
    }
}
