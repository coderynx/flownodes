using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

/// <summary>
///     Marker interface for a resource grain.
/// </summary>
public interface IResourceGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the resource summary.
    /// </summary>
    /// <returns>
    ///     <The resource summary./ returns>
    public ValueTask<ResourceSummary> GetSummary();

    /// <summary>
    ///     Get the resource kind.
    /// </summary>
    /// <returns>The resource kind.</returns>
    public ValueTask<string> GetKind();

    /// <summary>
    ///     Gets the resource ID.
    /// </summary>
    /// <returns>The resource ID.</returns>
    public ValueTask<string> GetId();

    /// <summary>
    ///     Gets the resource FRN,
    /// </summary>
    /// <returns>The resource FRN.</returns>
    ValueTask<string> GetFrn();

    /// <summary>
    ///     Updates the resource metadata.
    /// </summary>
    /// <param name="metadata">The new metadata to merge.</param>
    /// <returns></returns>
    Task UpdateMetadataAsync(Dictionary<string, string> metadata);

    /// <summary>
    ///     Configures the resource.
    /// </summary>
    /// <param name="configuration">The resource configuration to store.</param>
    Task UpdateConfigurationAsync(ResourceConfiguration configuration);

    /// <summary>
    ///     Gets the stored resource metadata.
    /// </summary>
    /// <returns>The stored metadata.</returns>
    ValueTask<Dictionary<string, string>> GetMetadata();

    /// <summary>
    ///     Clears the resource metadata.
    /// </summary>
    /// <returns></returns>
    Task ClearMetadataAsync();

    /// <summary>
    ///     Gets the stored configuration of the resource.
    /// </summary>
    /// <returns>The current resource configuration.</returns>
    ValueTask<ResourceConfiguration> GetConfiguration();

    /// <summary>
    ///     Clears the resource configuration.
    /// </summary>
    Task ClearConfigurationAsync();

    /// <summary>
    ///     Gets the stored state of the resource.
    /// </summary>
    /// <returns>Thea stored state of the resource.</returns>
    ValueTask<ResourceState> GetState();

    /// <summary>
    ///     Clears the state from the resource store.
    /// </summary>
    Task ClearStateAsync();

    /// <summary>
    ///     Clears the resource from the persistence store.
    /// </summary>
    Task SelfRemoveAsync();
}