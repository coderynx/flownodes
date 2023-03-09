using Flownodes.Shared.Interfaces;

namespace Flownodes.Shared.Models;

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

public record FlownodesId
{
    private static readonly Dictionary<Type, FlownodesObject> TypeToFlownodesObject = new()
    {
        { typeof(ITenantManagerGrain), FlownodesObject.TenantManager },
        { typeof(ITenantGrain), FlownodesObject.Tenant },
        { typeof(IAlertManagerGrain), FlownodesObject.AlertManager },
        { typeof(IAlertGrain), FlownodesObject.Alert },
        { typeof(IResourceManagerGrain), FlownodesObject.ResourceManager },
        { typeof(IDeviceGrain), FlownodesObject.Device },
        { typeof(IDataSourceGrain), FlownodesObject.DataSource },
        { typeof(IAssetGrain), FlownodesObject.Asset },
        { typeof(IScriptResourceGrain), FlownodesObject.Script },
        { typeof(IWorkflowManagerGrain), FlownodesObject.WorkflowManager },
        { typeof(IWorkflowGrain), FlownodesObject.Workflow }
    };

    private static readonly Dictionary<string, FlownodesObject> StringToFlownodesObject = new()
    {
        { ObjectNames.TenantManagerName, FlownodesObject.TenantManager },
        { ObjectNames.TenantName, FlownodesObject.Tenant },
        { ObjectNames.AlertManagerName, FlownodesObject.AlertManager },
        { ObjectNames.AlertName, FlownodesObject.Alert },
        { ObjectNames.ResourceManagerName, FlownodesObject.ResourceManager },
        { ObjectNames.DeviceName, FlownodesObject.Device },
        { ObjectNames.DataSourceName, FlownodesObject.DataSource },
        { ObjectNames.AssetName, FlownodesObject.Asset },
        { ObjectNames.ScriptName, FlownodesObject.Script },
        { ObjectNames.WorkflowManagerName, FlownodesObject.WorkflowManager },
        { ObjectNames.WorkflowName, FlownodesObject.Workflow },
        { ObjectNames.OtherName, FlownodesObject.Other }
    };

    private static readonly Dictionary<FlownodesObject, string> FlownodesObjectToString = new()
    {
        { FlownodesObject.TenantManager, "tenantManager" },
        { FlownodesObject.Tenant, "tenant" },
        { FlownodesObject.AlertManager, "alertManager" },
        { FlownodesObject.Alert, "alert" },
        { FlownodesObject.ResourceManager, "resourceManager" },
        { FlownodesObject.Device, "device" },
        { FlownodesObject.DataSource, "dataSource" },
        { FlownodesObject.Asset, "asset" },
        { FlownodesObject.Script, "script" },
        { FlownodesObject.WorkflowManager, "workflowManager" },
        { FlownodesObject.Workflow, "workflow" },
        { FlownodesObject.Other, "other" }
    };

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

    public FlownodesId(Type objectType, string firstName, string? secondName = null)
    {
        ArgumentNullException.ThrowIfNull(objectType);
        ArgumentException.ThrowIfNullOrEmpty(firstName);
        ArgumentException.ThrowIfNullOrEmpty(secondName);

        ObjectKind = KindFromType(objectType);
        if (IsManager && secondName is not null) throw new ArgumentException($"Passed invalid {nameof(objectType)}");

        FirstName = firstName;
        SecondName = secondName;
    }

    public FlownodesId(FlownodesObject objectKind, string firstName, string? secondName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(firstName);
        ArgumentException.ThrowIfNullOrEmpty(secondName);

        ObjectKind = objectKind;
        if (IsManager && secondName is not null) throw new ArgumentException($"Passed invalid {nameof(objectKind)}");

        FirstName = firstName;
        SecondName = secondName;
    }

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

    public string Id => ToString();

    public bool IsManager => ObjectKind is FlownodesObject.TenantManager
        or FlownodesObject.AlertManager
        or FlownodesObject.ResourceManager;

    public FlownodesObject ObjectKind { get; }
    public string FirstName { get; }
    public string? SecondName { get; }

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

    public static implicit operator FlownodesId(string id)
    {
        return new FlownodesId(id);
    }

    private static FlownodesObject KindFromType(Type type)
    {
        return TypeToFlownodesObject.TryGetValue(type, out var result) ? result : FlownodesObject.Other;
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
}