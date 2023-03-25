using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Authentication;
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

        var userManagerId = new FlownodesId(FlownodesEntity.UserManager, FlownodesEntityNames.UserManager);
        _userManager = grainFactory.GetGrain<IUserManagerGrain>(userManagerId);

        var rolesManagerId = new FlownodesId(FlownodesEntity.RoleClaimManager, FlownodesEntityNames.RoleClaimManager);
        _roleClaimManager = grainFactory.GetGrain<IRoleClaimManagerGrain>(rolesManagerId);

        var apiKeyManagerId = new FlownodesId(FlownodesEntity.ApiKeyManager, FlownodesEntityNames.ApiKeyManager);
        _apiKeyManager = grainFactory.GetGrain<IApiKeyManagerGrain>(apiKeyManagerId);

        var tenantManagerId = new FlownodesId(FlownodesEntity.TenantManager, FlownodesEntityNames.TenantManager);
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