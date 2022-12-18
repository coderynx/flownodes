using Flownodes.Core.Models;

namespace Flownodes.Core.Interfaces;

public interface IResourceManagerGrain : IGrainWithStringKey
{
    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string id) where TResourceGrain : IResourceGrain;

    ValueTask<TResource> DeployResourceAsync<TResource>(string id, ResourceConfiguration configuration)
        where TResource : IResourceGrain;

    Task RemoveResourceAsync(string id);
    Task RemoveAllResourcesAsync();
}