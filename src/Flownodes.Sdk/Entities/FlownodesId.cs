namespace Flownodes.Sdk.Entities;

/// <summary>
///     Enumeration of all Flownodes entities.
/// </summary>
public enum FlownodesEntity
{
    TenantManager,
    Tenant,
    AlertManager,
    Alert,
    ResourceManager,
    ResourceGroup,
    Device,
    DataSource,
    Asset,
    Script,
    Other
}

/// <summary>
///     The Flownodes ID is used to represent an entity in the Flownodes cluster.
/// </summary>
public record FlownodesId
{
    private static readonly Dictionary<string, FlownodesEntity> StringToFlownodesEntity = new()
    {
        { FlownodesEntityNames.TenantManager, FlownodesEntity.TenantManager },
        { FlownodesEntityNames.Tenant, FlownodesEntity.Tenant },
        { FlownodesEntityNames.AlertManager, FlownodesEntity.AlertManager },
        { FlownodesEntityNames.Alert, FlownodesEntity.Alert },
        { FlownodesEntityNames.ResourceManager, FlownodesEntity.ResourceManager },
        { FlownodesEntityNames.ResourceGroup, FlownodesEntity.ResourceGroup },
        { FlownodesEntityNames.Device, FlownodesEntity.Device },
        { FlownodesEntityNames.DataSource, FlownodesEntity.DataSource },
        { FlownodesEntityNames.Asset, FlownodesEntity.Asset },
        { FlownodesEntityNames.Script, FlownodesEntity.Script },
        { FlownodesEntityNames.Other, FlownodesEntity.Other }
    };

    private static readonly Dictionary<FlownodesEntity, string> FlownodesEntityToString = new()
    {
        { FlownodesEntity.TenantManager, FlownodesEntityNames.TenantManager },
        { FlownodesEntity.Tenant, FlownodesEntityNames.Tenant },
        { FlownodesEntity.AlertManager, FlownodesEntityNames.AlertManager },
        { FlownodesEntity.Alert, FlownodesEntityNames.Alert },
        { FlownodesEntity.ResourceManager, FlownodesEntityNames.ResourceManager },
        { FlownodesEntity.ResourceGroup, FlownodesEntityNames.ResourceGroup },
        { FlownodesEntity.Device, FlownodesEntityNames.Device },
        { FlownodesEntity.DataSource, FlownodesEntityNames.DataSource },
        { FlownodesEntity.Asset, FlownodesEntityNames.Asset },
        { FlownodesEntity.Script, FlownodesEntityNames.Script },
        { FlownodesEntity.Other, FlownodesEntityNames.Other }
    };

    /// <summary>
    ///     Creates a Flownodes ID from a string entityKind.
    /// </summary>
    /// <param name="entityKind">The entity kind string to create the ID from.</param>
    /// <param name="firstName">The first part of the ID.</param>
    /// <param name="secondName">The second part of the ID.</param>
    public FlownodesId(string entityKind, string firstName, string? secondName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityKind);
        ArgumentException.ThrowIfNullOrEmpty(firstName);

        EntityKind = KindFromString(entityKind);
        if (IsShort && secondName is not null) throw new ArgumentException($"Passed invalid {nameof(entityKind)}");

        FirstName = firstName;
        SecondName = secondName;
    }

    /// <summary>
    ///     Creates a Flownodes ID from an enum EntityKind.
    /// </summary>
    /// <param name="entityKind">The entityKind enum to create the ID from.</param>
    /// <param name="firstName">The first part of the ID.</param>
    /// <param name="secondName">Te secondo part of the ID.</param>
    public FlownodesId(FlownodesEntity entityKind, string firstName, string? secondName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(firstName);

        EntityKind = entityKind;
        if (IsShort && secondName is not null) throw new ArgumentException($"Passed invalid {nameof(entityKind)}");

        FirstName = firstName;
        SecondName = secondName;
    }

    /// <summary>
    ///     Creates the Flownodes ID from the string representation.
    /// </summary>
    /// <param name="id">The string to create the Flownodes ID from.</param>
    public FlownodesId(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        if (id.Contains('/'))
        {
            var tokens = id.Split('/');
            FirstName = tokens[0];

            var secondName = tokens[1].Split(':');
            EntityKind = KindFromString(secondName[0]);
            if (IsShort) throw new ArgumentException($"Passed invalid {nameof(id)}");

            SecondName = secondName[1];

            return;
        }

        if (!id.Contains(':')) throw new ArgumentException("Passed invalid FlownodesId string");

        var firstName = id.Split(':');
        EntityKind = KindFromString(firstName[0]);
        if (!IsShort) throw new ArgumentException($"Passed invalid {nameof(id)}");

        FirstName = firstName[1];
    }

    /// <summary>
    ///     The string representation of the Flownodes ID.
    /// </summary>
    public string IdString => ToString();

    /// <summary>
    ///     If the Flownodes ID refers to a manager.
    /// </summary>
    public bool IsManager => EntityKind is FlownodesEntity.TenantManager
        or FlownodesEntity.AlertManager
        or FlownodesEntity.ResourceManager;

    private bool IsShort => IsManager || EntityKind == FlownodesEntity.Tenant;

    /// <summary>
    ///     The entity to which the Flownodes ID refers to.
    /// </summary>
    public FlownodesEntity EntityKind { get; }

    /// <summary>
    ///     The first part of the Flownodes ID.
    /// </summary>
    public string FirstName { get; }

    /// <summary>
    ///     The second part of the Flownodes ID.
    /// </summary>
    public string? SecondName { get; }

    /// <summary>
    ///     Gets the string representation of the Flownodes ID.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        return !IsShort
            ? $"{FirstName}/{KindToString(EntityKind)}:{SecondName}"
            : $"{KindToString(EntityKind)}:{FirstName}";
    }

    public static implicit operator string(FlownodesId id)
    {
        return id.ToString();
    }

    public static explicit operator FlownodesId(string id)
    {
        return new FlownodesId(id);
    }

    private static FlownodesEntity KindFromString(string kind)
    {
        if (StringToFlownodesEntity.TryGetValue(kind, out var result)) return result;

        throw new ArgumentException($"Provided invalid {nameof(kind)}");
    }

    private static string KindToString(FlownodesEntity kind)
    {
        if (FlownodesEntityToString.TryGetValue(kind, out var result)) return result;

        throw new ArgumentException($"Provided invalid {nameof(kind)}");
    }

    public string ToEntityKindString()
    {
        return KindToString(EntityKind);
    }
}