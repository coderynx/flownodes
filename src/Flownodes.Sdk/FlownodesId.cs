namespace Flownodes.Sdk;

/// <summary>
///     Enumeration of all the Flownodes objects.
/// </summary>
public enum FlownodesObject
{
    TenantManager,
    Tenant,
    AlertManager,
    Alert,
    ResourceManager,
    Device,
    DataSource,
    Asset,
    Script,
    WorkflowManager,
    Workflow,
    Other
}

/// <summary>
///     The Flownodes ID is used to represent an object in the Flownodes cluster.
/// </summary>
public record FlownodesId
{
    private static readonly Dictionary<string, FlownodesObject> StringToFlownodesObject = new()
    {
        { FlownodesObjectNames.TenantManager, FlownodesObject.TenantManager },
        { FlownodesObjectNames.Tenant, FlownodesObject.Tenant },
        { FlownodesObjectNames.AlertManager, FlownodesObject.AlertManager },
        { FlownodesObjectNames.Alert, FlownodesObject.Alert },
        { FlownodesObjectNames.ResourceManager, FlownodesObject.ResourceManager },
        { FlownodesObjectNames.Device, FlownodesObject.Device },
        { FlownodesObjectNames.DataSource, FlownodesObject.DataSource },
        { FlownodesObjectNames.Asset, FlownodesObject.Asset },
        { FlownodesObjectNames.Script, FlownodesObject.Script },
        { FlownodesObjectNames.WorkflowManager, FlownodesObject.WorkflowManager },
        { FlownodesObjectNames.Workflow, FlownodesObject.Workflow },
        { FlownodesObjectNames.Other, FlownodesObject.Other }
    };

    private static readonly Dictionary<FlownodesObject, string> FlownodesObjectToString = new()
    {
        { FlownodesObject.TenantManager, FlownodesObjectNames.TenantManager },
        { FlownodesObject.Tenant, FlownodesObjectNames.Tenant },
        { FlownodesObject.AlertManager, FlownodesObjectNames.AlertManager },
        { FlownodesObject.Alert, FlownodesObjectNames.Alert },
        { FlownodesObject.ResourceManager, FlownodesObjectNames.ResourceManager },
        { FlownodesObject.Device, FlownodesObjectNames.Device },
        { FlownodesObject.DataSource, FlownodesObjectNames.DataSource },
        { FlownodesObject.Asset, FlownodesObjectNames.Asset },
        { FlownodesObject.Script, FlownodesObjectNames.Script },
        { FlownodesObject.WorkflowManager, FlownodesObjectNames.WorkflowManager },
        { FlownodesObject.Workflow, FlownodesObjectNames.Workflow },
        { FlownodesObject.Other, FlownodesObjectNames.Other }
    };

    /// <summary>
    ///     Creates a Flownodes ID from a string objectKind.
    /// </summary>
    /// <param name="objectKind">The object kind string to create the ID from.</param>
    /// <param name="firstName">The first part of the ID.</param>
    /// <param name="secondName">The second part of the ID.</param>
    public FlownodesId(string objectKind, string firstName, string? secondName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(objectKind);
        ArgumentException.ThrowIfNullOrEmpty(firstName);
        ArgumentException.ThrowIfNullOrEmpty(secondName);

        ObjectKind = KindFromString(objectKind);
        if (IsManager && secondName is not null) throw new ArgumentException($"Passed invalid {nameof(objectKind)}");

        FirstName = firstName;
        SecondName = secondName;
    }

    /// <summary>
    ///     Creates a Flownodes ID from an enum ObjectKind.
    /// </summary>
    /// <param name="objectKind">The objectKind enum to create the ID from.</param>
    /// <param name="firstName">The first part of the ID.</param>
    /// <param name="secondName">Te secondo part of the ID.</param>
    public FlownodesId(FlownodesObject objectKind, string firstName, string? secondName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(firstName);
        ArgumentException.ThrowIfNullOrEmpty(secondName);

        ObjectKind = objectKind;
        if (IsManager && secondName is not null) throw new ArgumentException($"Passed invalid {nameof(objectKind)}");

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
            ObjectKind = KindFromString(secondName[0]);
            if (IsManager) throw new ArgumentException($"Passed invalid {nameof(id)}");

            SecondName = secondName[1];

            return;
        }

        if (id.Contains(':')) throw new ArgumentException("Passed invalid FlownodesId string");

        var firstName = id.Split(':');
        ObjectKind = KindFromString(firstName[0]);
        if (!IsManager) throw new ArgumentException($"Passed invalid {nameof(id)}");

        FirstName = firstName[1];

        throw new ArgumentException("Passed invalid FlownodesId string");
    }

    /// <summary>
    ///     The string representation of the Flownodes ID.
    /// </summary>
    public string IdString => ToString();

    /// <summary>
    ///     If the Flownodes ID refers to a manager.
    /// </summary>
    public bool IsManager => ObjectKind is FlownodesObject.TenantManager
        or FlownodesObject.AlertManager
        or FlownodesObject.ResourceManager;

    /// <summary>
    ///     The object to which the Flownodes ID refers to.
    /// </summary>
    public FlownodesObject ObjectKind { get; }

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
        return !IsManager
            ? $"{FirstName}/{KindToString(ObjectKind)}:{SecondName}"
            : $"{KindToString(ObjectKind)}:{SecondName}";
    }

    public static implicit operator string(FlownodesId id)
    {
        return id.ToString();
    }

    public static explicit operator FlownodesId(string id)
    {
        return new FlownodesId(id);
    }

    private static FlownodesObject KindFromString(string kind)
    {
        if (StringToFlownodesObject.TryGetValue(kind, out var result)) return result;

        throw new ArgumentException($"Provided invalid {nameof(kind)}");
    }

    private static string KindToString(FlownodesObject kind)
    {
        if (FlownodesObjectToString.TryGetValue(kind, out var result)) return result;

        throw new ArgumentException($"Provided invalid {nameof(kind)}");
    }

    public string ToObjectKindString()
    {
        return KindToString(ObjectKind);
    }
}