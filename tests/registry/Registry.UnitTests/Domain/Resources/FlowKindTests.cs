using Registry.Application.Resources;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class FlowKindTests
{
    private const string Valid = """
                                 {"executionMode": "queue", "limits": {"executions": 3, "steps": 50, "variableBytes": 4096}, "nodes": [
                                   {"id": "hot", "type": "trigger",
                                    "dataPoint": {"asset": "room", "name": "temperature"},
                                    "when": {"operator": ">", "value": 25}, "next": "read"},
                                   {"id": "pressed", "type": "trigger", "event": {"device": "button", "name": "toggled"},
                                    "next": "read"},
                                   {"id": "nightly", "type": "trigger",
                                    "schedule": {"cron": "0 2 * * *", "timeZone": "Europe/Rome", "maxLateness": "10m"},
                                    "next": "read"},
                                   {"id": "manual", "type": "trigger", "next": "read"},
                                   {"id": "read", "type": "read", "dataPoint": {"device": "fan", "name": "on"},
                                    "into": "fan", "next": "check", "timeout": "5s", "retry": {"times": 2, "delay": "1s"}},
                                   {"id": "check", "type": "branch", "if": {"from": "fan.value", "operator": "==", "value": false},
                                    "then": "start", "else": "settle"},
                                   {"id": "start", "type": "write", "dataPoint": {"device": "fan", "name": "on"}, "value": true,
                                    "next": "cool", "error": "settle"},
                                   {"id": "cool", "type": "invoke", "action": {"device": "thermostat", "name": "set"},
                                    "parameters": {"value": 21}, "into": "result", "next": "settle"},
                                   {"id": "settle", "type": "wait", "for": "5m", "maxLateness": "1m", "next": "until"},
                                   {"id": "until", "type": "wait",
                                    "until": {"dataPoint": {"asset": "room", "name": "temperature"}, "operator": "<", "value": 22},
                                    "timeout": "30m", "timedOut": "copy", "next": "event"},
                                   {"id": "event", "type": "wait", "event": {"device": "button", "name": "toggled"},
                                    "timeout": "1h", "into": "press"},
                                   {"id": "copy", "type": "write", "dataPoint": {"asset": "room", "name": "target"},
                                    "from": "trigger.value"}]}
                                 """;

    private static readonly NamespaceName Acme = NamespaceName.Create("acme");

    [Fact]
    public void Validate_ShouldAcceptTheSpec_WhenItWiresEveryNodeType()
    {
        // Act
        var errors = new FlowKind(LampCatalog.Surfaces()).Validate(Widgets.Json(Valid), Snapshot());

        // Assert
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"nodes": [], "extra": 1}""", "spec.extra")]
    [InlineData("""{"limits": {"steps": 0}, "nodes": [{"id": "a", "type": "trigger"}]}""", "spec.limits.steps")]
    [InlineData(
        """{"limits": {"executions": 101}, "nodes": [{"id": "a", "type": "trigger"}]}""",
        "spec.limits.executions")]
    [InlineData("""{"limits": {"forever": 1}, "nodes": [{"id": "a", "type": "trigger"}]}""", "spec.limits.forever")]
    [InlineData("""{"nodes": []}""", "spec.nodes")]
    [InlineData("""{}""", "spec.nodes")]
    [InlineData("""{"executionMode": "burst", "nodes": [{"id": "a", "type": "trigger"}]}""", "spec.executionMode")]
    [InlineData("""{"nodes": [{"id": "a", "type": "read", "dataPoint": {"asset": "x", "name": "y"}}]}""", "spec.nodes")]
    [InlineData("""{"nodes": [{"id": "a", "type": "trigger"}, {"id": "a", "type": "trigger"}]}""", "spec.nodes[1].id")]
    [InlineData("""{"nodes": [{"id": "Bad Id", "type": "trigger"}]}""", "spec.nodes[0].id")]
    [InlineData("""{"nodes": [{"id": "a\n", "type": "trigger"}]}""", "spec.nodes[0].id")]
    [InlineData("""{"nodes": [{"id": "t", "type": "trigger"}, {"id": "a", "type": "loop"}]}""", "spec.nodes[1].type")]
    [InlineData("""{"nodes": [{"id": "a", "type": "trigger", "next": "nowhere"}]}""", "spec.nodes[0].next")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "next": "b"}, {"id": "b", "type": "trigger"}]}""",
        "spec.nodes[0].next")]
    [InlineData("""{"nodes": [{"id": "a", "type": "trigger", "typo": 1}]}""", "spec.nodes[0].typo")]
    [InlineData("""{"nodes": [{"id": "a", "type": "trigger", "error": "a"}]}""", "spec.nodes[0].error")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "event": {"device": "b", "name": "c"}, "schedule": {"cron": "* * * * *"}}]}""",
        "spec.nodes[0]")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "dataPoint": {"asset": "x", "device": "y", "name": "z"}}]}""",
        "spec.nodes[0].dataPoint")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "dataPoint": {"asset": "x"}}]}""",
        "spec.nodes[0].dataPoint.name")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "when": {"operator": ">", "value": 1}}]}""",
        "spec.nodes[0].when")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "dataPoint": {"asset": "x", "name": "y"}, "when": {"operator": "~", "value": 1}}]}""",
        "spec.nodes[0].when.operator")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "dataPoint": {"asset": "x", "name": "y"}, "when": {"operator": ">"}}]}""",
        "spec.nodes[0].when.value")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "event": {"asset": "x", "name": "y"}}]}""",
        "spec.nodes[0].event")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "event": {"device": "x", "connection": "y", "name": "z"}}]}""",
        "spec.nodes[0].event")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "dataPoint": {"connection": "x", "name": "y"}}]}""",
        "spec.nodes[0].dataPoint.connection")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "schedule": {"cron": "61 * * * *"}}]}""",
        "spec.nodes[0].schedule.cron")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "schedule": {"cron": "* * * * *", "timeZone": "Mars/Olympus"}}]}""",
        "spec.nodes[0].schedule.timeZone")]
    [InlineData(
        """{"nodes": [{"id": "a", "type": "trigger", "schedule": {"cron": "* * * * *", "maxLateness": "soon"}}]}""",
        "spec.nodes[0].schedule.maxLateness")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "read"}]}""",
        "spec.nodes[1].dataPoint")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "read", "dataPoint": {"asset": "x", "name": "y"}, "into": "trigger"}]}""",
        "spec.nodes[1].into")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "read", "dataPoint": {"asset": "x", "name": "y"}, "timeout": "0s"}]}""",
        "spec.nodes[1].timeout")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "read", "dataPoint": {"asset": "x", "name": "y"}, "timeout": "5s\n"}]}""",
        "spec.nodes[1].timeout")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "read", "dataPoint": {"asset": "x", "name": "y"}, "into": "fan\n"}]}""",
        "spec.nodes[1].into")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "read", "dataPoint": {"asset": "x", "name": "y"}, "retry": {"times": -1}}]}""",
        "spec.nodes[1].retry.times")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "write", "dataPoint": {"asset": "x", "name": "y"}}]}""",
        "spec.nodes[1]")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "write", "dataPoint": {"asset": "x", "name": "y"}, "value": 1, "from": "trigger"}]}""",
        "spec.nodes[1]")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "write", "dataPoint": {"asset": "x", "name": "y"}, "from": "a..b"}]}""",
        "spec.nodes[1].from")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "write", "dataPoint": {"asset": "x", "name": "y"}, "from": "trigger.value\n"}]}""",
        "spec.nodes[1].from")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "invoke", "action": {"asset": "x", "name": "y"}}]}""",
        "spec.nodes[1].action")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "invoke", "action": {"device": "x", "name": "y"}, "parameters": 1}]}""",
        "spec.nodes[1].parameters")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "branch", "if": {"operator": "==", "value": 1}}]}""",
        "spec.nodes[1].if.from")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "branch", "if": {"from": "x", "operator": "==", "value": 1}, "next": "t"}]}""",
        "spec.nodes[1].next")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "wait"}]}""",
        "spec.nodes[1]")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "wait", "event": {"device": "x", "name": "y"}}]}""",
        "spec.nodes[1].timeout")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "wait", "until": {"dataPoint": {"asset": "x", "name": "y"}, "operator": "<", "value": 1}}]}""",
        "spec.nodes[1].timeout")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "wait", "for": "1m", "timeout": "1m"}]}""",
        "spec.nodes[1].timeout")]
    [InlineData(
        """{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {"id": "a", "type": "wait", "for": "1m", "retry": {"times": 1}}]}""",
        "spec.nodes[1].retry")]
    public void Validate_ShouldReportThePath_WhenTheSpecIsInvalid(string json, string path)
    {
        // Arrange
        var kind = new FlowKind(LampCatalog.Surfaces());

        // Act
        var errors = kind.Validate(Widgets.Json(json), Snapshot()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    [Fact]
    public void References_ShouldListEveryAssetAndDeviceOnce_WhenNodesNameThem()
    {
        // Act
        var references = new FlowKind(LampCatalog.Surfaces()).References(Widgets.Json(Valid)).ToList();

        // Assert
        references.Select(reference => (reference.Kind, reference.Name))
            .ShouldBe([("Asset", "room"), ("Device", "button"), ("Device", "fan"), ("Device", "thermostat")], true);
        references.ShouldContain(reference => reference.Path == "spec.nodes[0].dataPoint.asset");
    }

    [Fact]
    public void Validate_ShouldAcceptTheNames_WhenTheReferencedResourcesOfferThem()
    {
        // Arrange
        var spec = Widgets.Json(
            """
            {"nodes": [
              {"id": "t", "type": "trigger", "event": {"device": "bulb", "name": "burnt"}, "next": "a"},
              {"id": "a", "type": "read", "dataPoint": {"asset": "room", "name": "target"}, "next": "b"},
              {"id": "b", "type": "invoke", "action": {"device": "bulb", "name": "blink"}, "next": "c"},
              {"id": "c", "type": "wait", "timeout": "1m",
               "until": {"dataPoint": {"device": "bulb", "name": "level"}, "operator": ">", "value": 1}}]}
            """);

        // Act
        var errors = new FlowKind(LampCatalog.Surfaces()).Validate(spec, Offering());

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ShouldAcceptTheNames_WhenTheReferencedConnectionOffersThem()
    {
        // Arrange
        var spec = Widgets.Json(
            """
            {"nodes": [
              {"id": "t", "type": "trigger", "event": {"connection": "hub", "name": "outage"}, "next": "a"},
              {"id": "a", "type": "invoke", "action": {"connection": "hub", "name": "forecast"}, "next": "b"},
              {"id": "b", "type": "wait", "timeout": "1m", "event": {"connection": "hub", "name": "outage"}}]}
            """);

        // Act
        var errors = new FlowKind(LampCatalog.Surfaces()).Validate(spec, Offering());

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void References_ShouldListTheConnection_WhenANodeNamesItsAction()
    {
        // Arrange
        var spec = Widgets.Json(
            """
            {"nodes": [{"id": "t", "type": "trigger", "next": "a"},
              {"id": "a", "type": "invoke", "action": {"connection": "hub", "name": "forecast"}}]}
            """);

        // Act
        var references = new FlowKind(LampCatalog.Surfaces()).References(spec).ToList();

        // Assert
        references.ShouldHaveSingleItem()
            .ShouldBe(new SpecReference("Connection", "hub", "spec.nodes[1].action.connection"));
    }

    [Theory]
    [InlineData(
        """{"id": "a", "type": "read", "dataPoint": {"asset": "room", "name": "speed"}}""",
        "spec.nodes[1].dataPoint.name")]
    [InlineData(
        """{"id": "a", "type": "read", "dataPoint": {"device": "bulb", "name": "speed"}}""",
        "spec.nodes[1].dataPoint.name")]
    [InlineData(
        """{"id": "a", "type": "invoke", "action": {"device": "bulb", "name": "explode"}}""",
        "spec.nodes[1].action.name")]
    [InlineData(
        """{"id": "a", "type": "wait", "timeout": "1m", "event": {"device": "bulb", "name": "sang"}}""",
        "spec.nodes[1].event.name")]
    [InlineData(
        """{"id": "a", "type": "invoke", "action": {"connection": "hub", "name": "explode"}}""",
        "spec.nodes[1].action.name")]
    [InlineData(
        """{"id": "a", "type": "wait", "timeout": "1m", "event": {"connection": "hub", "name": "sang"}}""",
        "spec.nodes[1].event.name")]
    [InlineData(
        """{"id": "a", "type": "wait", "timeout": "1m", "until": {"dataPoint": {"asset": "room", "name": "speed"}, "operator": ">", "value": 1}}""",
        "spec.nodes[1].until.dataPoint.name")]
    public void Validate_ShouldReportTheName_WhenTheReferencedResourceDoesNotOfferIt(string node, string path)
    {
        // Arrange
        var spec = Widgets.Json($$"""{"nodes": [{"id": "t", "type": "trigger", "next": "a"}, {{node}}]}""");

        // Act
        var errors = new FlowKind(LampCatalog.Surfaces()).Validate(spec, Offering()).ToList();

        // Assert
        errors.ShouldContain(error => error.Path == path);
    }

    private static NamespaceSnapshot Snapshot() => new(Acme, []);

    private static NamespaceSnapshot Offering() =>
        new(
            Acme,
            [
                new SnapshotResource(
                    "AssetType",
                    ResourceName.Create("office"),
                    Widgets.Json("""{"dataPoints": [{"name": "target", "type": "number"}]}""")),
                new SnapshotResource(
                    "Asset",
                    ResourceName.Create("room"),
                    Widgets.Json(
                        """{"assetType": "office", "dataPoints": [{"name": "temperature", "type": "number"}]}""")),
                new SnapshotResource(
                    "Connection",
                    ResourceName.Create("hub"),
                    Widgets.Json("""{"integration": "acme.lamp", "config": {"host": "x"}}""")),
                new SnapshotResource(
                    "Device",
                    ResourceName.Create("bulb"),
                    Widgets.Json("""{"connection": "hub", "type": "Bulb", "address": {"id": 3}}"""))
            ]);
}
