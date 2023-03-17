using Flownodes.Sdk;
using Flownodes.Shared.Alerting;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Scripts;
using Flownodes.Shared.Tenanting;
using Flownodes.Shared.Workflows;

namespace Flownodes.Worker.Builders;

public static class FlownodesIdBuilder
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
        { typeof(IScriptGrain), FlownodesObject.Script },
        { typeof(IWorkflowManagerGrain), FlownodesObject.WorkflowManager },
        { typeof(IWorkflowGrain), FlownodesObject.Workflow }
    };

    private static FlownodesObject KindFromType(this Type type)
    {
        return TypeToFlownodesObject.TryGetValue(type, out var result) ? result : FlownodesObject.Other;
    }

    public static FlownodesId CreateFromType(Type objectType, string firstName, string? secondName = null)
    {
        return new FlownodesId(KindFromType(objectType), firstName, secondName);
    }
}