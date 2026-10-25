using System.Text.Json;

namespace Flownodes.Sdk;

/// <summary>
/// The type of a value in a <see cref="ConfigSchema"/>.
/// </summary>
public enum ConfigFieldType
{
    /// <summary>
    /// A text.
    /// </summary>
    String,

    /// <summary>
    /// A whole number.
    /// </summary>
    Integer,

    /// <summary>
    /// Any number.
    /// </summary>
    Number,

    /// <summary>
    /// <c>true</c> or <c>false</c>.
    /// </summary>
    Boolean
}

/// <summary>
/// One field of a <see cref="ConfigSchema"/>.
/// </summary>
/// <param name="Name">The property name.</param>
/// <param name="Type">The type its value has.</param>
/// <param name="Required">Whether the field must be present.</param>
public sealed record ConfigField(string Name, ConfigFieldType Type, bool Required = false);

/// <summary>
/// One problem found in a configuration.
/// </summary>
/// <param name="Path">The property the problem is about, relative to the configuration.</param>
/// <param name="Message">What is wrong.</param>
public sealed record ConfigProblem(string Path, string Message);

/// <summary>
/// The shape of a configuration object an Integration accepts: flat, with known fields only.
/// </summary>
/// <param name="Fields">The fields the configuration may hold.</param>
public sealed record ConfigSchema(IReadOnlyList<ConfigField> Fields)
{
    /// <summary>
    /// Gets a schema that accepts only an empty object.
    /// </summary>
    public static ConfigSchema Empty { get; } = new([]);

    /// <summary>
    /// Checks a configuration against the schema.
    /// </summary>
    /// <param name="config">The configuration, which must be an object.</param>
    /// <returns>Every problem found; empty when the configuration is valid.</returns>
    public IReadOnlyList<ConfigProblem> Validate(JsonElement config)
    {
        if (config.ValueKind != JsonValueKind.Object)
        {
            return [new ConfigProblem(string.Empty, "Must be an object.")];
        }

        var problems = new List<ConfigProblem>();
        foreach (var property in config.EnumerateObject())
        {
            var field = Fields.FirstOrDefault(candidate => candidate.Name == property.Name);
            if (field is null)
            {
                problems.Add(new ConfigProblem(property.Name, "Unknown field."));
            }
            else if (!Accepts(field.Type, property.Value))
            {
                problems.Add(
                    new ConfigProblem(property.Name, $"Must be of type {field.Type.ToString().ToLowerInvariant()}."));
            }
        }

        problems.AddRange(
            Fields.Where(field => field.Required && !config.TryGetProperty(field.Name, out _))
                .Select(field => new ConfigProblem(field.Name, "Required.")));

        return problems;
    }

    private static bool Accepts(ConfigFieldType type, JsonElement value) =>
        type switch
        {
            ConfigFieldType.String => value.ValueKind == JsonValueKind.String,
            ConfigFieldType.Integer => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            ConfigFieldType.Number => value.ValueKind == JsonValueKind.Number,
            ConfigFieldType.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            _ => false
        };
}
