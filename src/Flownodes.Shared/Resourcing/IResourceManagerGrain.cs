using System.Collections.ObjectModel;

namespace Flownodes.Shared.Resourcing;

public interface IResourceManagerGrain : IGrainWithStringKey
{
    ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string resourceName)
        where TResourceGrain : IResourceGrain;

    ValueTask<ReadOnlyCollection<ResourceSummary>> GetAllResourceSummaries();

    ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string resourceName,
        Dictionary<string, object?>? configuration = null,
        Dictionary<string, string?>? metadata = null)
        where TResourceGrain : IResourceGrain;

    Task RemoveResourceAsync(string resourceName);
    Task RemoveAllResourcesAsync();
    ValueTask<ResourceSummary?> GetResourceSummary(string resourceName);
    ValueTask<IResourceGrain?> GetGenericResourceAsync(string resourceName);
    ValueTask<IReadOnlyList<IResourceGrain>> SearchResourcesByTags(HashSet<string> tags);
}