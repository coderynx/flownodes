using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Authentication;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Shared.Tenanting.Grains;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Runtime;

namespace Flownodes.Worker.Services;

public interface IEnvironmentService
{
    string ServiceId { get; }
    string ClusterId { get; }
    int SilosCount { get; }

    ITenantManagerGrain GetTenantManager();
    Task<IResourceManagerGrain?> GetResourceManager(string tenantName);
    Task<IAlertManagerGrain?> GetAlertManager(string tenantName);
    Task<IEventBookGrain?> GetEventBook(string tenantName);
    IUserManagerGrain GetUserManager();
    IApiKeyManagerGrain GetApiKeyManager();
    IRoleClaimManagerGrain GetRoleClaimManager();
}

public class EnvironmentService : IEnvironmentService
{
    private readonly IApiKeyManagerGrain _apiKeyManager;
    private readonly IClusterManifestProvider _clusterManifestProvider;
    private readonly ClusterOptions _clusterOptions;
    private readonly IRoleClaimManagerGrain _roleClaimManager;
    private readonly ITenantManagerGrain _tenantManager;
    private readonly IUserManagerGrain _userManager;

    public EnvironmentService(IOptions<ClusterOptions> clusterOptions, IGrainFactory grainFactory,
        IClusterManifestProvider clusterManifestProvider)
    {
        _clusterOptions = clusterOptions.Value;

        var userManagerId = new EntityId(Entity.UserManager, EntityNames.UserManager);
        _userManager = grainFactory.GetGrain<IUserManagerGrain>(userManagerId);

        var rolesManagerId = new EntityId(Entity.RoleClaimManager, EntityNames.RoleClaimManager);
        _roleClaimManager = grainFactory.GetGrain<IRoleClaimManagerGrain>(rolesManagerId);

        var apiKeyManagerId = new EntityId(Entity.ApiKeyManager, EntityNames.ApiKeyManager);
        _apiKeyManager = grainFactory.GetGrain<IApiKeyManagerGrain>(apiKeyManagerId);

        var tenantManagerId = new EntityId(Entity.TenantManager, EntityNames.TenantManager);
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(tenantManagerId);

        _clusterManifestProvider = clusterManifestProvider;
    }

    public string ServiceId => _clusterOptions.ServiceId;
    public string ClusterId => _clusterOptions.ClusterId;
    public int SilosCount => _clusterManifestProvider.Current.Silos.Count;

    public IUserManagerGrain GetUserManager()
    {
        return _userManager;
    }

    public IApiKeyManagerGrain GetApiKeyManager()
    {
        return _apiKeyManager;
    }

    public IRoleClaimManagerGrain GetRoleClaimManager()
    {
        return _roleClaimManager;
    }

    public ITenantManagerGrain GetTenantManager()
    {
        return _tenantManager;
    }

    public async Task<IEventBookGrain?> GetEventBook(string tenantName)
    {
        var tenant = await _tenantManager.GetTenantAsync(tenantName);
        if (tenant is null) return null;

        return await tenant.GetEventBook();
    }

    public async Task<IResourceManagerGrain?> GetResourceManager(string tenantName)
    {
        var tenant = await _tenantManager.GetTenantAsync(tenantName);
        if (tenant is null) return null;

        return await tenant.GetResourceManager();
    }

    public async Task<IAlertManagerGrain?> GetAlertManager(string tenantName)
    {
        var tenant = await _tenantManager.GetTenantAsync(tenantName);
        if (tenant is null) return null;

        return await tenant.GetAlertManager();
    }
}