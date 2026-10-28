using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Domain.Scheduling;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal sealed partial class FlowKind(DeviceSurfaces surfaces) : IResourceKind
{
    public const string KindName = "Flow";
    private static readonly string[] Modes = ["single", "restart", "queue", "parallel"];
    private static readonly string[] Operators = ["<", "<=", ">", ">=", "==", "!="];
    private static readonly string[] Edges = ["next", "error", "then", "else", "timedOut"];
    private static readonly string[] Reserved = ["trigger", "error"];
    private static readonly string[] Calls = ["next", "error", "timeout", "retry"];

    private static readonly Dictionary<string, string[]> Fields = new()
    {
        ["trigger"] = ["next", "dataPoint", "when", "event", "schedule"],
        ["read"] = [.. Calls, "dataPoint", "into"],
        ["write"] = [.. Calls, "dataPoint", "value", "from"],
        ["invoke"] = [.. Calls, "action", "parameters", "from", "into"],
        ["branch"] = ["if", "then", "else"],
        ["wait"] = ["next", "error", "timedOut", "for", "until", "event", "timeout", "maxLateness", "into"]
    };

    private static readonly Dictionary<string, int> Ceilings = new()
    {
        ["executions"] = 100,
        ["steps"] = 100_000,
        ["variableBytes"] = 1024 * 1024
    };

    public string Name => KindName;
    public string Plural => "flows";

    public IEnumerable<SpecError> Validate(JsonElement spec, NamespaceSnapshot @namespace)
    {
        foreach (var field in spec.EnumerateObject()
                     .Where(field => field.Name is not ("executionMode" or "limits" or "nodes")))
        {
            yield return new SpecError($"spec.{field.Name}", "Unknown field.");
        }

        if (spec.TryGetProperty("executionMode", out _) && !Modes.Contains(Text(spec, "executionMode")))
        {
            yield return new SpecError("spec.executionMode", $"Must be one of {string.Join(", ", Modes)}.");
        }

        foreach (var error in Limits(spec))
        {
            yield return error;
        }

        var nodes = Nodes(spec);
        if (nodes.Count == 0 || nodes.TrueForAll(node => Text(node, "type") != "trigger"))
        {
            yield return new SpecError("spec.nodes", "Required, as a list of Nodes with at least one trigger.");

            yield break;
        }

        var types = new Dictionary<string, string?>();
        for (var index = 0; index < nodes.Count; index++)
        {
            var path = $"spec.nodes[{index}]";
            var id = Text(nodes[index], "id");
            if (id is null || !NodeId().IsMatch(id))
            {
                yield return new SpecError($"{path}.id", "Required, as lowercase letters, digits and dashes.");
            }
            else if (!types.TryAdd(id, Text(nodes[index], "type")))
            {
                yield return new SpecError($"{path}.id", $"Node '{id}' is declared twice.");
            }
        }

        for (var index = 0; index < nodes.Count; index++)
        {
            foreach (var error in ValidateNode(nodes[index], $"spec.nodes[{index}]", types))
            {
                yield return error;
            }

            foreach (var error in Offered(nodes[index], $"spec.nodes[{index}]", @namespace))
            {
                yield return error;
            }
        }
    }

    public IEnumerable<SpecReference> References(JsonElement spec)
    {
        var seen = new HashSet<(string, string)>();
        var nodes = Nodes(spec);
        for (var index = 0; index < nodes.Count; index++)
        {
            var path = $"spec.nodes[{index}]";
            var node = nodes[index];
            var targets = new[]
            {
                (Ref: Object(node, "dataPoint"), Path: $"{path}.dataPoint"),
                (Ref: Object(node, "event"), Path: $"{path}.event"),
                (Ref: Object(node, "action"), Path: $"{path}.action"),
                (Ref: Object(Object(node, "until"), "dataPoint"), Path: $"{path}.until.dataPoint")
            };
            foreach (var (target, at) in targets)
            {
                foreach (var (kind, field) in new[] { (AssetKind.KindName, "asset"), (DeviceKind.KindName, "device") })
                {
                    if (Text(target, field) is { } name && seen.Add((kind, name)))
                    {
                        yield return new SpecReference(kind, name, $"{at}.{field}");
                    }
                }
            }
        }
    }

    private IEnumerable<SpecError> Offered(JsonElement node, string path, NamespaceSnapshot @namespace)
    {
        var targets = new[]
        {
            (Target: Object(node, "dataPoint"), Path: $"{path}.dataPoint", Member: "Data Point"),
            (Target: Object(node, "event"), Path: $"{path}.event", Member: "Event"),
            (Target: Object(node, "action"), Path: $"{path}.action", Member: "Action"),
            (Target: Object(Object(node, "until"), "dataPoint"), Path: $"{path}.until.dataPoint", Member: "Data Point")
        };

        return targets.Select(target => Unoffered(target.Target, target.Path, target.Member, @namespace))
            .OfType<SpecError>();
    }

    private SpecError? Unoffered(JsonElement target, string path, string member, NamespaceSnapshot @namespace)
    {
        if (Text(target, "name") is not { } name)
        {
            return null;
        }

        var (owner, offered) = Text(target, "asset") is { } asset
            ? ($"Asset '{asset}'", AssetDataPoints(asset, @namespace))
            : ($"Device '{Text(target, "device")}'", DeviceMembers(Text(target, "device"), member, @namespace));

        return offered is null || offered.Contains(name)
            ? null
            : new SpecError($"{path}.name", $"{owner} has no {member} '{name}'.");
    }

    private static HashSet<string>? AssetDataPoints(string name, NamespaceSnapshot @namespace)
    {
        if (@namespace.Find(AssetKind.KindName, name) is not { } asset)
        {
            return null;
        }

        var type = Text(asset, "assetType") is { } typeName ? @namespace.Find(AssetTypeKind.KindName, typeName) : null;

        var inherited = type is { } declaring ? DataPointDeclarations.Names(declaring) : [];

        return [.. DataPointDeclarations.Names(asset), .. inherited];
    }

    private HashSet<string>? DeviceMembers(string? device, string member, NamespaceSnapshot @namespace)
    {
        if (device is null || surfaces.Find(@namespace, device) is not { } surface)
        {
            return null;
        }

        return member switch
        {
            "Event" => [.. surface.Events],
            "Action" => [.. surface.Actions],
            _ => [.. surface.DataPoints.Select(dataPoint => dataPoint.Name)]
        };
    }

    private static IEnumerable<SpecError> ValidateNode(JsonElement node, string path, Dictionary<string, string?> types)
    {
        var type = Text(node, "type");
        if (type is null || !Fields.TryGetValue(type, out var allowed))
        {
            yield return new SpecError($"{path}.type", $"Required, as one of {string.Join(", ", Fields.Keys)}.");

            yield break;
        }

        foreach (var field in node.EnumerateObject()
                     .Where(field => field.Name is not ("id" or "type") && !allowed.Contains(field.Name)))
        {
            yield return new SpecError($"{path}.{field.Name}", "Unknown field.");
        }

        foreach (var edge in Edges.Where(edge => allowed.Contains(edge)))
        {
            if (!node.TryGetProperty(edge, out var target))
            {
                continue;
            }

            var name = target.ValueKind == JsonValueKind.String ? target.GetString()! : "";
            if (!types.TryGetValue(name, out var targetType))
            {
                yield return new SpecError($"{path}.{edge}", "Must be the id of another Node of this Flow.");
            }
            else if (targetType == "trigger")
            {
                yield return new SpecError($"{path}.{edge}", $"Node '{name}' is a trigger, which nothing leads into.");
            }
        }

        var errors = type switch
        {
            "trigger" => Trigger(node, path),
            "read" => Call(node, path).Concat(DataPoint(node, path, true)).Concat(Into(node, path)),
            "write" => Call(node, path).Concat(DataPoint(node, path, true)).Concat(ValueOrFrom(node, path, "value")),
            "invoke" => Call(node, path)
                .Concat(DeviceMember(node, "action", path, true))
                .Concat(Parameters(node, path))
                .Concat(Into(node, path)),
            "branch" => Comparison(node, "if", path, true),
            _ => Wait(node, path)
        };
        foreach (var error in errors)
        {
            yield return error;
        }
    }

    private static IEnumerable<SpecError> Limits(JsonElement spec)
    {
        if (!spec.TryGetProperty("limits", out var limits))
        {
            yield break;
        }

        if (limits.ValueKind != JsonValueKind.Object)
        {
            yield return new SpecError("spec.limits", "Must be an object of executions, steps and variableBytes.");

            yield break;
        }

        foreach (var field in limits.EnumerateObject())
        {
            var value = field.Value.TryGetInt32(out var given) ? given : 0;
            if (!Ceilings.TryGetValue(field.Name, out var ceiling))
            {
                yield return new SpecError($"spec.limits.{field.Name}", "Unknown field.");
            }
            else if (value < 1 || value > ceiling)
            {
                yield return new SpecError($"spec.limits.{field.Name}", $"Must be a whole number from 1 to {ceiling}.");
            }
        }
    }

    private static IEnumerable<SpecError> Trigger(JsonElement node, string path)
    {
        var sources = new[] { "dataPoint", "event", "schedule" }.Count(field => node.TryGetProperty(field, out _));
        if (sources > 1)
        {
            yield return new SpecError(path, "A trigger takes at most one of dataPoint, event and schedule.");
        }

        foreach (var error in DataPoint(node, path, false).Concat(DeviceMember(node, "event", path, false)))
        {
            yield return error;
        }

        if (node.TryGetProperty("when", out _))
        {
            var errors = node.TryGetProperty("dataPoint", out _)
                ? Comparison(node, "when", path, false)
                : [new SpecError($"{path}.when", "Only a dataPoint trigger takes a condition.")];
            foreach (var error in errors)
            {
                yield return error;
            }
        }

        if (!node.TryGetProperty("schedule", out var schedule))
        {
            yield break;
        }

        if (schedule.ValueKind != JsonValueKind.Object)
        {
            yield return new SpecError($"{path}.schedule", "Must be an object with a cron expression.");

            yield break;
        }

        foreach (var field in schedule.EnumerateObject()
                     .Where(field => field.Name is not ("cron" or "timeZone" or "maxLateness")))
        {
            yield return new SpecError($"{path}.schedule.{field.Name}", "Unknown field.");
        }

        if (Text(schedule, "cron") is not { } cron || !CronSchedule.TryCreate(cron, out _))
        {
            yield return new SpecError(
                $"{path}.schedule.cron",
                "Required, as a cron expression of five fields, or six with seconds first.");
        }

        if (schedule.TryGetProperty("timeZone", out var zone) && !IsTimeZone(zone))
        {
            yield return new SpecError($"{path}.schedule.timeZone", "Must be an IANA time zone such as Europe/Rome.");
        }

        if (schedule.TryGetProperty("maxLateness", out var lateness) && !Durations.IsPositive(lateness))
        {
            yield return new SpecError($"{path}.schedule.maxLateness", Durations.Hint);
        }
    }

    private static IEnumerable<SpecError> Wait(JsonElement node, string path)
    {
        var kinds = new[] { "for", "until", "event" }.Where(field => node.TryGetProperty(field, out _)).ToList();
        if (kinds.Count != 1)
        {
            yield return new SpecError(path, "A wait takes exactly one of for, until and event.");

            yield break;
        }

        var errors = kinds[0] switch
        {
            "for" when !Durations.IsPositive(node.GetProperty("for")) => [new SpecError($"{path}.for", Durations.Hint)],
            "for" => [],
            "until" => Until(node, path),
            _ => DeviceMember(node, "event", path, true)
        };
        foreach (var error in errors)
        {
            yield return error;
        }

        var hasTimeout = node.TryGetProperty("timeout", out var timeout);
        if (kinds[0] == "for" && hasTimeout)
        {
            yield return new SpecError($"{path}.timeout", "A wait for a duration takes no timeout.");
        }
        else if (kinds[0] != "for" && !hasTimeout)
        {
            yield return new SpecError($"{path}.timeout", "Required for a wait on a condition or an Event.");
        }
        else if (hasTimeout && !Durations.IsPositive(timeout))
        {
            yield return new SpecError($"{path}.timeout", Durations.Hint);
        }

        if (node.TryGetProperty("maxLateness", out var lateness) && !Durations.IsPositive(lateness))
        {
            yield return new SpecError($"{path}.maxLateness", Durations.Hint);
        }

        if (kinds[0] != "event" && node.TryGetProperty("into", out _))
        {
            yield return new SpecError($"{path}.into", "Only a wait for an Event keeps a value.");
        }

        foreach (var error in Into(node, path))
        {
            yield return error;
        }
    }

    private static IEnumerable<SpecError> Until(JsonElement node, string path)
    {
        var until = node.GetProperty("until");
        if (until.ValueKind != JsonValueKind.Object)
        {
            return [new SpecError($"{path}.until", "Must be a Data Point, an operator and a value.")];
        }

        var unknown = until.EnumerateObject()
            .Where(field => field.Name is not ("dataPoint" or "operator" or "value"))
            .Select(field => new SpecError($"{path}.until.{field.Name}", "Unknown field."));

        return unknown.Concat(DataPoint(until, $"{path}.until", true)).Concat(Operation(until, $"{path}.until"));
    }

    private static IEnumerable<SpecError> Call(JsonElement node, string path)
    {
        if (node.TryGetProperty("timeout", out var timeout) && !Durations.IsPositive(timeout))
        {
            yield return new SpecError($"{path}.timeout", Durations.Hint);
        }

        if (!node.TryGetProperty("retry", out var retry))
        {
            yield break;
        }

        if (retry.ValueKind != JsonValueKind.Object)
        {
            yield return new SpecError($"{path}.retry", "Must be an object with attempts and an optional delay.");

            yield break;
        }

        foreach (var field in retry.EnumerateObject().Where(field => field.Name is not ("times" or "delay")))
        {
            yield return new SpecError($"{path}.retry.{field.Name}", "Unknown field.");
        }

        var times = retry.TryGetProperty("times", out var given) && given.TryGetInt32(out var count) ? count : -1;
        if (times < 0)
        {
            yield return new SpecError($"{path}.retry.times", "Required, as how many times to retry, from 0.");
        }

        if (retry.TryGetProperty("delay", out var delay) && !Durations.IsPositive(delay))
        {
            yield return new SpecError($"{path}.retry.delay", Durations.Hint);
        }
    }

    private static IEnumerable<SpecError> DataPoint(JsonElement node, string path, bool required)
    {
        if (!node.TryGetProperty("dataPoint", out var target))
        {
            return required
                ?
                [
                    new SpecError(
                        $"{path}.dataPoint",
                        "Required, as an asset or device and the name of its Data Point.")
                ]
                : [];
        }

        var errors = Member(target, $"{path}.dataPoint", ["asset", "device"]).ToList();
        var owners = new[] { "asset", "device" }.Count(owner => Text(target, owner) is not null);
        if (target.ValueKind == JsonValueKind.Object && owners != 1)
        {
            errors.Add(new SpecError($"{path}.dataPoint", "Takes exactly one of asset and device."));
        }

        return errors;
    }

    private static IEnumerable<SpecError> DeviceMember(JsonElement node, string field, string path, bool required)
    {
        if (!node.TryGetProperty(field, out var target))
        {
            return required ? [new SpecError($"{path}.{field}", "Required, as a device and a name.")] : [];
        }

        var errors = Member(target, $"{path}.{field}", ["device"]).ToList();
        if (target.ValueKind == JsonValueKind.Object && Text(target, "device") is null)
        {
            errors.Add(new SpecError($"{path}.{field}.device", "Required, as the name of a Device."));
        }

        return errors;
    }

    private static IEnumerable<SpecError> Member(JsonElement target, string path, string[] owners)
    {
        if (target.ValueKind != JsonValueKind.Object)
        {
            yield return new SpecError(path, "Must be an object.");

            yield break;
        }

        foreach (var field in target.EnumerateObject()
                     .Where(field => field.Name != "name" && !owners.Contains(field.Name)))
        {
            yield return new SpecError($"{path}.{field.Name}", "Unknown field.");
        }

        foreach (var owner in owners.Where(owner =>
                     target.TryGetProperty(owner, out var value) && value.ValueKind != JsonValueKind.String))
        {
            yield return new SpecError($"{path}.{owner}", "Must be the name of a Resource.");
        }

        if (string.IsNullOrWhiteSpace(Text(target, "name")))
        {
            yield return new SpecError($"{path}.name", "Required.");
        }
    }

    private static IEnumerable<SpecError> Comparison(JsonElement node, string field, string path, bool takesFrom)
    {
        var comparison = node.TryGetProperty(field, out var given) ? given : default;
        if (comparison.ValueKind != JsonValueKind.Object)
        {
            return [new SpecError($"{path}.{field}", "Required, as an operator and a value.")];
        }

        string[] known = takesFrom ? ["from", "operator", "value"] : ["operator", "value"];
        var unknown = comparison.EnumerateObject()
            .Where(entry => !known.Contains(entry.Name))
            .Select(entry => new SpecError($"{path}.{field}.{entry.Name}", "Unknown field."));
        var from = takesFrom && !IsPath(comparison, "from")
            ? [new SpecError($"{path}.{field}.from", "Required, as a variable such as trigger.value.")]
            : Array.Empty<SpecError>();

        return unknown.Concat(from).Concat(Operation(comparison, $"{path}.{field}"));
    }

    private static IEnumerable<SpecError> Operation(JsonElement comparison, string path)
    {
        if (Text(comparison, "operator") is not { } op || !Operators.Contains(op))
        {
            yield return new SpecError($"{path}.operator", $"Required, as one of {string.Join(", ", Operators)}.");
        }

        if (!comparison.TryGetProperty("value", out _))
        {
            yield return new SpecError($"{path}.value", "Required.");
        }
    }

    private static IEnumerable<SpecError> ValueOrFrom(JsonElement node, string path, string literal)
    {
        var hasValue = node.TryGetProperty(literal, out _);
        var hasFrom = node.TryGetProperty("from", out _);
        if (hasValue == hasFrom)
        {
            yield return new SpecError(path, $"Takes exactly one of {literal} and from.");
        }
        else if (hasFrom && !IsPath(node, "from"))
        {
            yield return new SpecError($"{path}.from", "Must be a variable such as trigger.value.");
        }
    }

    private static IEnumerable<SpecError> Parameters(JsonElement node, string path)
    {
        if (node.TryGetProperty("parameters", out var parameters) && parameters.ValueKind != JsonValueKind.Object)
        {
            return [new SpecError($"{path}.parameters", "Must be an object.")];
        }

        return node.TryGetProperty("parameters", out _) ? ValueOrFrom(node, path, "parameters") : FromOnly(node, path);
    }

    private static IEnumerable<SpecError> FromOnly(JsonElement node, string path) =>
        node.TryGetProperty("from", out _) && !IsPath(node, "from")
            ? [new SpecError($"{path}.from", "Must be a variable such as trigger.value.")]
            : [];

    private static IEnumerable<SpecError> Into(JsonElement node, string path)
    {
        if (!node.TryGetProperty("into", out _))
        {
            yield break;
        }

        if (Text(node, "into") is not { } name || !Variable().IsMatch(name))
        {
            yield return new SpecError($"{path}.into", "Must be a variable name of letters, digits and underscores.");
        }
        else if (Reserved.Contains(name))
        {
            yield return new SpecError($"{path}.into", $"'{name}' is set by the Flow Execution itself.");
        }
    }

    private static bool IsPath(JsonElement node, string field) =>
        Text(node, field) is { } path && VariablePath().IsMatch(path);

    private static bool IsTimeZone(JsonElement zone) =>
        zone.ValueKind == JsonValueKind.String && TimeZoneInfo.TryFindSystemTimeZoneById(zone.GetString()!, out _);

    private static List<JsonElement> Nodes(JsonElement spec) =>
        spec.TryGetProperty("nodes", out var list) && list.ValueKind == JsonValueKind.Array
            ? [.. list.EnumerateArray().Where(node => node.ValueKind == JsonValueKind.Object)]
            : [];

    private static JsonElement Object(JsonElement node, string name) =>
        node.ValueKind == JsonValueKind.Object && node.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Object
            ? value
            : default;

    private static string? Text(JsonElement node, string name) =>
        node.ValueKind == JsonValueKind.Object && node.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$")]
    private static partial Regex NodeId();

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex Variable();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z0-9_]+)*$")]
    private static partial Regex VariablePath();
}
