using System.Text.Json;
using Core.Application.Integrations;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

/// <summary>
/// The rules for where an Asset Data Point gets its value (ADR-0002, ADR-0010): mapped from a Device Data Point,
/// computed from other Data Points of the Asset, or, without a source, written by a Flow.
/// </summary>
internal sealed class AssetSources(DeviceSurfaces surfaces)
{
    private static readonly string[] Operations = ["sum", "min", "max", "average"];

    private static Dictionary<string, Declaration> First(IEnumerable<Declaration> declarations) =>
        declarations.GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

    public static IEnumerable<SpecError> ValidateAssetType(JsonElement spec)
    {
        var declarations = DataPointDeclarations.Declarations(spec);
        var effective = First(declarations);
        foreach (var declaration in declarations.Where(declaration => declaration.Source is not null))
        {
            var path = $"{declaration.Path}.source";
            foreach (var error in ValidateSource(
                         declaration.Source!.Value,
                         path,
                         declaration,
                         effective,
                         allowDevice: false,
                         null))
            {
                yield return error;
            }
        }

        foreach (var error in Cycles(effective, name => effective.GetValueOrDefault(name)?.Source))
        {
            yield return error;
        }
    }

    public IEnumerable<SpecError> ValidateAsset(JsonElement spec, JsonElement? assetType, NamespaceSnapshot @namespace)
    {
        var inherited = assetType is { } type ? DataPointDeclarations.Declarations(type) : [];
        var own = DataPointDeclarations.Declarations(spec);
        var effective = First([.. inherited, .. own]);
        var overrides = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var context = new Context(@namespace, surfaces);

        foreach (var error in ValidateOverrides(spec, inherited, effective, overrides, context))
        {
            yield return error;
        }

        foreach (var declaration in own.Where(declaration => declaration.Source is not null))
        {
            foreach (var error in ValidateSource(
                         declaration.Source!.Value,
                         $"{declaration.Path}.source",
                         declaration,
                         effective,
                         allowDevice: true,
                         context))
            {
                yield return error;
            }
        }

        foreach (var error in Cycles(
                     effective,
                     name => overrides.TryGetValue(name, out var source)
                         ? source
                         : effective.GetValueOrDefault(name)?.Source,
                     overrides.Keys.ToHashSet(StringComparer.Ordinal),
                     own.Select(declaration => declaration.Name).ToHashSet(StringComparer.Ordinal)))
        {
            yield return error;
        }
    }

    public static IEnumerable<SpecReference> References(JsonElement spec)
    {
        foreach (var declaration in DataPointDeclarations.Declarations(spec))
        {
            if (declaration.Source is { } source && DeviceName(source) is { } device)
            {
                yield return new SpecReference(DeviceKind.KindName, device, $"{declaration.Path}.source.device");
            }
        }

        if (spec.TryGetProperty("sources", out var sources) && sources.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in sources.EnumerateObject())
            {
                if (DeviceName(entry.Value) is { } device)
                {
                    yield return new SpecReference(DeviceKind.KindName, device, $"spec.sources.{entry.Name}.device");
                }
            }
        }
    }

    private static IEnumerable<SpecError> ValidateOverrides(
        JsonElement spec,
        IReadOnlyList<Declaration> inherited,
        Dictionary<string, Declaration> effective,
        Dictionary<string, JsonElement> overrides,
        Context context)
    {
        if (!spec.TryGetProperty("sources", out var sources))
        {
            yield break;
        }

        if (sources.ValueKind != JsonValueKind.Object)
        {
            yield return new SpecError("spec.sources", "Must map the name of an Asset Type Data Point to its source.");

            yield break;
        }

        foreach (var entry in sources.EnumerateObject())
        {
            var path = $"spec.sources.{entry.Name}";
            if (inherited.All(declaration => declaration.Name != entry.Name))
            {
                yield return new SpecError(path, $"The Asset Type declares no Data Point '{entry.Name}'.");

                continue;
            }

            overrides[entry.Name] = entry.Value;
            foreach (var error in ValidateSource(
                         entry.Value,
                         path,
                         effective[entry.Name],
                         effective,
                         allowDevice: true,
                         context))
            {
                yield return error;
            }
        }
    }

    private static IEnumerable<SpecError> ValidateSource(
        JsonElement source,
        string path,
        Declaration declared,
        Dictionary<string, Declaration> effective,
        bool allowDevice,
        Context? context)
    {
        if (source.ValueKind != JsonValueKind.Object)
        {
            yield return new SpecError(path, "Must be a device mapping or a computation.");

            yield break;
        }

        foreach (var field in source.EnumerateObject()
                     .Where(field => field.Name is not ("device" or "dataPoint" or "compute")))
        {
            yield return new SpecError($"{path}.{field.Name}", "Unknown field.");
        }

        var isCompute = source.TryGetProperty("compute", out var compute);
        var isDevice = source.TryGetProperty("device", out _) || source.TryGetProperty("dataPoint", out _);
        if (isCompute == isDevice)
        {
            yield return new SpecError(path, "Must be either a device and dataPoint, or a compute.");

            yield break;
        }

        if (isCompute)
        {
            foreach (var error in ValidateCompute(compute, $"{path}.compute", declared, effective))
            {
                yield return error;
            }

            yield break;
        }

        if (!allowDevice)
        {
            yield return new SpecError(
                path,
                "An Asset Type cannot map from a Device; each Asset maps it in spec.sources.");

            yield break;
        }

        foreach (var error in ValidateDevice(source, path, declared, context!))
        {
            yield return error;
        }
    }

    private static IEnumerable<SpecError> ValidateCompute(
        JsonElement compute,
        string path,
        Declaration declared,
        Dictionary<string, Declaration> effective)
    {
        if (compute.ValueKind != JsonValueKind.Object)
        {
            yield return new SpecError(path, "Must be an object with an op and inputs.");

            yield break;
        }

        foreach (var field in compute.EnumerateObject().Where(field => field.Name is not ("op" or "inputs")))
        {
            yield return new SpecError($"{path}.{field.Name}", "Unknown field.");
        }

        if (Text(compute, "op") is not { } op || !Operations.Contains(op))
        {
            yield return new SpecError($"{path}.op", $"Required, as one of {string.Join(", ", Operations)}.");
        }

        if (declared.Type != "number")
        {
            yield return new SpecError(path, $"Data Point '{declared.Name}' is computed, so it must be a number.");
        }

        if (declared.Writable)
        {
            yield return new SpecError(path, $"Data Point '{declared.Name}' is computed, so it cannot be writable.");
        }

        if (!IsNameList(compute, out var inputs))
        {
            yield return new SpecError($"{path}.inputs", "Required, as a list of Data Point names.");

            yield break;
        }

        var index = 0;
        foreach (var name in inputs.EnumerateArray().Select(input => input.GetString()!))
        {
            var inputPath = $"{path}.inputs[{index++}]";
            if (name == declared.Name)
            {
                yield return new SpecError(inputPath, "A Data Point cannot be computed from itself.");
            }
            else if (!effective.TryGetValue(name, out var input))
            {
                yield return new SpecError(inputPath, $"There is no Data Point '{name}'.");
            }
            else if (input.Type is not ("integer" or "number"))
            {
                yield return new SpecError(inputPath, $"Data Point '{name}' is not a number.");
            }
        }
    }

    private static bool IsNameList(JsonElement compute, out JsonElement inputs) =>
        compute.TryGetProperty("inputs", out inputs) && inputs.ValueKind == JsonValueKind.Array &&
        inputs.GetArrayLength() > 0 && inputs.EnumerateArray().All(input => input.ValueKind == JsonValueKind.String);

    private static IEnumerable<SpecError> ValidateDevice(
        JsonElement source,
        string path,
        Declaration declared,
        Context context)
    {
        var deviceName = Text(source, "device");
        var dataPointName = Text(source, "dataPoint");
        if (deviceName is null)
        {
            yield return new SpecError($"{path}.device", "Required, as the name of a Device.");
        }

        if (dataPointName is null)
        {
            yield return new SpecError($"{path}.dataPoint", "Required, as the name of a Data Point of the Device.");
        }

        if (deviceName is null || dataPointName is null)
        {
            yield break;
        }

        if (context.Surface(deviceName) is not { } surface)
        {
            yield break;
        }

        var dataPoint = surface.FirstOrDefault(candidate => candidate.Name == dataPointName);
        if (dataPoint is null)
        {
            yield return new SpecError(
                $"{path}.dataPoint",
                $"Device '{deviceName}' has no Data Point '{dataPointName}'.");

            yield break;
        }

        if (declared.Type is { } type && type != dataPoint.Type)
        {
            yield return new SpecError(
                $"{path}.dataPoint",
                $"Data Point '{dataPointName}' is a {dataPoint.Type}, but '{declared.Name}' is declared as a {type}.");
        }

        if (declared.Writable && !dataPoint.Writable)
        {
            yield return new SpecError(
                $"{path}.dataPoint",
                $"Data Point '{dataPointName}' is read-only, but '{declared.Name}' is declared writable.");
        }
    }

    private static IEnumerable<SpecError> Cycles(
        Dictionary<string, Declaration> effective,
        Func<string, JsonElement?> sourceOf,
        HashSet<string>? overridden = null,
        HashSet<string>? own = null)
    {
        foreach (var name in effective.Keys)
        {
            if (!Reaches(name, name, sourceOf, new HashSet<string>(StringComparer.Ordinal)))
            {
                continue;
            }

            var path = $"{effective[name].Path}.source";
            if (overridden?.Contains(name) == true)
            {
                path = $"spec.sources.{name}";
            }
            else if (own is not null && !own.Contains(name))
            {
                path = "spec.assetType";
            }

            yield return new SpecError(path, $"Data Point '{name}' is computed from itself through other Data Points.");
        }
    }

    private static bool Reaches(
        string from,
        string target,
        Func<string, JsonElement?> sourceOf,
        HashSet<string> visited)
    {
        foreach (var input in Inputs(sourceOf(from)))
        {
            if (input == target)
            {
                return true;
            }

            if (visited.Add(input) && Reaches(input, target, sourceOf, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> Inputs(JsonElement? source) =>
        source is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty("compute", out var compute) &&
        compute.ValueKind == JsonValueKind.Object && compute.TryGetProperty("inputs", out var inputs) &&
        inputs.ValueKind == JsonValueKind.Array
            ? inputs.EnumerateArray()
                .Where(input => input.ValueKind == JsonValueKind.String)
                .Select(input => input.GetString()!)
            : [];

    private static string? DeviceName(JsonElement source) =>
        source.ValueKind == JsonValueKind.Object ? Text(source, "device") : null;

    private static string? Text(JsonElement spec, string name) =>
        spec.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private sealed record Context(NamespaceSnapshot Namespace, DeviceSurfaces Surfaces)
    {
        public IReadOnlyList<DeviceDataPointSchema>? Surface(string deviceName) =>
            Surfaces.Find(Namespace, deviceName)?.DataPoints;
    }
}
