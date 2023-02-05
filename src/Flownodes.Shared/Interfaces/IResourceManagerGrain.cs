using System.Collections.ObjectModel;
using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

public interface IResourceManagerGrain : IGrainWithStringKey
{
    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string id) where TResourceGrain : IResourceGrain;
    ValueTask<ReadOnlyCollection<Resource>> GetAllResourceSummaries();

    ValueTask<string> DeployResourceAsync<TResource>(string name, ResourceConfigurationStore configurationStore)
        where TResource : IResourceGrain;

    Task RemoveResourceAsync(string id);
    Task RemoveAllResourcesAsync();
    ValueTask<Resource?> GetResourceSummary(string id);
    ValueTask<IResourceGrain?> GetGenericResourceAsync(string id);
}