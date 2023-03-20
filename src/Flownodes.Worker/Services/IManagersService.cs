using Flownodes.Shared.Alerting;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Tenanting;

namespace Flownodes.Worker.Services;

public interface IManagersService
{
    ITenantManagerGrain GetTenantManager();
    Task<IResourceManagerGrain?> GetResourceManager(string tenantName);
    Task<IAlertManagerGrain?> GetAlertManager(string tenantName);
}