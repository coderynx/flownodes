using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Authentication;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Shared.Tenanting.Grains;

namespace Flownodes.Worker.Builders;

public static class FlownodesIdBuilder
{
    private static readonly Dictionary<Type, Entity> TypeToFlownodesObject = new()
    {
        { typeof(IUserManagerGrain), Entity.UserManager },
        { typeof(IRoleClaimManagerGrain), Entity.RoleClaimManager },
        { typeof(IApiKeyManagerGrain), Entity.ApiKeyManager },
        { typeof(ITenantGrain), Entity.Tenant },
        { typeof(IAlertManagerGrain), Entity.AlertManager },
        { typeof(IAlertGrain), Entity.Alert },
        { typeof(IEventBookGrain), Entity.EventBook },
        { typeof(IResourceManagerGrain), Entity.ResourceManager },
        { typeof(IResourceGroupGrain), Entity.ResourceGroup },
        { typeof(IDeviceZoneGrain), Entity.DeviceZone },
        { typeof(IDeviceGrain), Entity.Device },
        { typeof(IDataSourceGrain), Entity.DataSource },
        { typeof(IAssetGrain), Entity.Asset },
        { typeof(IScriptGrain), Entity.Script }
    };

    private static Entity GetKindFromType(Type type)
    {
        return TypeToFlownodesObject.TryGetValue(type, out var result) ? result : Entity.Other;
    }

    public static EntityId CreateFromType<TResourceGrain>(string firstName, string? secondName = null) where TResourceGrain : IResourceGrain
    {
        return new EntityId(GetKindFromType(typeof(TResourceGrain)), firstName, secondName);
    }
}