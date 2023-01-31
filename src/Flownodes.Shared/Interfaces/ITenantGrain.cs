using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

public interface ITenantGrain : IGrainWithStringKey
{
    Task UpdateConfigurationAsync(TenantConfiguration configuration);
    ValueTask<TenantConfiguration> GetConfiguration();

    ValueTask<IResourceManagerGrain> GetResourceManager();
    ValueTask<IAlertManagerGrain> GetAlertManager();
    ValueTask<IWorkflowManagerGrain> GetWorkflowManager();
}