using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Authentication;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Shared.Tenanting.Grains;

namespace Flownodes.Worker.Builders;

public static class FlownodesIdBuilder
{
    private static readonly Dictionary<Type, FlownodesEntity> TypeToFlownodesObject = new()
    {
        { typeof(IUserManagerGrain), FlownodesEntity.UserManager },
        { typeof(IRoleClaimManagerGrain), FlownodesEntity.RoleClaimManager },
        { typeof(IApiKeyManagerGrain), FlownodesEntity.ApiKeyManager },
        { typeof(ITenantGrain), FlownodesEntity.Tenant },
        { typeof(IAlertManagerGrain), FlownodesEntity.AlertManager },
        { typeof(IAlertGrain), FlownodesEntity.Alert },
        { typeof(IEventBookGrain), FlownodesEntity.EventBook },
        { typeof(IResourceManagerGrain), FlownodesEntity.ResourceManager },
        { typeof(IResourceGroupGrain), FlownodesEntity.ResourceGroup },
        { typeof(IDeviceZoneGrain), FlownodesEntity.DeviceZone },
        { typeof(IDeviceGrain), FlownodesEntity.Device },
        { typeof(IDataSourceGrain), FlownodesEntity.DataSource },
        { typeof(IAssetGrain), FlownodesEntity.Asset },
        { typeof(IScriptGrain), FlownodesEntity.Script }
    };

    private static FlownodesEntity GetKindFromType(Type type)
    {
        return TypeToFlownodesObject.TryGetValue(type, out var result) ? result : FlownodesEntity.Other;
    }

    public static FlownodesId CreateFromType(Type objectType, string firstName, string? secondName = null)
    {
        return new FlownodesId(GetKindFromType(objectType), firstName, secondName);
    }
}