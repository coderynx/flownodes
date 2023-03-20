using Flownodes.Sdk;
using Flownodes.Shared.Alerting;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Scripts;
using Flownodes.Shared.Tenanting;

namespace Flownodes.Worker.Builders;

public static class FlownodesIdBuilder
{
    private static readonly Dictionary<Type, FlownodesEntity> TypeToFlownodesObject = new()
    {
        { typeof(ITenantManagerGrain), FlownodesEntity.TenantManager },
        { typeof(ITenantGrain), FlownodesEntity.Tenant },
        { typeof(IAlertManagerGrain), FlownodesEntity.AlertManager },
        { typeof(IAlertGrain), FlownodesEntity.Alert },
        { typeof(IResourceManagerGrain), FlownodesEntity.ResourceManager },
        { typeof(IDeviceGrain), FlownodesEntity.Device },
        { typeof(IDataSourceGrain), FlownodesEntity.DataSource },
        { typeof(IAssetGrain), FlownodesEntity.Asset },
        { typeof(IScriptGrain), FlownodesEntity.Script }
    };

    private static FlownodesEntity KindFromType(this Type type)
    {
        return TypeToFlownodesObject.TryGetValue(type, out var result) ? result : FlownodesEntity.Other;
    }

    public static FlownodesId CreateFromType(Type objectType, string firstName, string? secondName = null)
    {
        return new FlownodesId(KindFromType(objectType), firstName, secondName);
    }
}