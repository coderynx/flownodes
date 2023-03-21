using System.Collections.ObjectModel;
using Flownodes.Shared.Entities;

namespace Flownodes.Shared.Resourcing.Grains;

public interface IResourceManagerGrain : IEntityGrain
{
    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string name)
        where TResourceGrain : IResourceGrain;

    ValueTask<ReadOnlyCollection<ResourceSummary>> GetAllResourceSummaries();

    ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string resourceName,
        Dictionary<string, object?>? configuration = null,
        Dictionary<string, string?>? metadata = null)
        where TResourceGrain : IResourceGrain;

    Task RemoveResourceAsync(string resourceName);
    Task RemoveAllResourcesAsync();
    ValueTask<ResourceSummary?> GetResourceSummary(string resourceName);
    ValueTask<IResourceGrain?> GetResourceAsync(string name);
    ValueTask<IReadOnlyList<IResourceGrain>> SearchResourcesByTags(HashSet<string> tags);
}