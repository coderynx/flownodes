using System.Collections.ObjectModel;
using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

public interface IResourceManagerGrain : IGrainWithStringKey
{
    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string tenantName, string resourceName) where TResourceGrain : IResourceGrain;
    ValueTask<ReadOnlyCollection<Resource>> GetAllResourceSummaries(string tenantName);

    ValueTask<string> DeployResourceAsync<TResource>(string tenantName, string resourceName, ResourceConfigurationStore configurationStore)
        where TResource : IResourceGrain;

    Task RemoveResourceAsync(string tenantName, string resourceName);
    Task RemoveAllResourcesAsync(string tenantName);
    ValueTask<Resource?> GetResourceSummary(string tenantName, string resourceName);
    ValueTask<IResourceGrain?> GetGenericResourceAsync(string tenantName, string resourceName);
}