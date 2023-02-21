using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

/// <summary>
///     Marker interface for a resource grain.
/// </summary>
public interface IResourceGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the resource POCO.
    /// </summary>
    /// <returns>The resource POCO. </returns>
    public ValueTask<Resource> GetPoco();

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
    ///     Updates the resource metadata.
    /// </summary>
    /// <param name="properties">The new metadata to merge.</param>
    /// <returns></returns>
    Task UpdateMetadataAsync(Dictionary<string, string?> properties);

    /// <summary>
    ///     Configures the resource.
    /// </summary>
    /// <param name="configurationStore">The resource configuration to store.</param>
    Task UpdateConfigurationAsync(ResourceConfigurationStore configurationStore);

    /// <summary>
    ///     Updates the resource current configuration.
    /// </summary>
    /// <param name="properties">The properties to set.</param>
    /// <param name="behaviorId">The behavior id to set.</param>
    /// <returns></returns>
    Task UpdateConfigurationAsync(Dictionary<string, object?>? properties, string behaviorId);

    /// <summary>
    ///     Updates the resource current configuration.
    /// </summary>
    /// <param name="properties">The properties to set to the resource.</param>
    /// <returns></returns>
    Task UpdateConfigurationAsync(Dictionary<string, object?> properties);

    /// <summary>
    ///     Updates the state of the resource by storing the new state and applying it.
    /// </summary>
    /// <param name="properties">The new state of the resource to store.</param>
    Task UpdateStateAsync(Dictionary<string, object?> properties);

    /// <summary>
    ///     Gets the stored resource metadata.
    /// </summary>
    /// <returns>The stored metadata.</returns>
    ValueTask<ResourceMetadataStore> GetMetadata();

    /// <summary>
    /// Retrieves the stored resource metadata properties.
    /// </summary>
    /// <returns>The metadata properties of the resource.</returns>
    ValueTask<Dictionary<string, string?>> GetMetadataProperties();

    /// <summary>
    ///     Clears the resource metadata.
    /// </summary>
    /// <returns></returns>
    Task ClearMetadataAsync();

    /// <summary>
    ///     Gets the stored configuration of the resource.
    /// </summary>
    /// <returns>The current resource configuration.</returns>
    ValueTask<ResourceConfigurationStore> GetConfiguration();

    /// <summary>
    /// Retrieves the stored configuration properties of the resource.
    /// </summary>
    /// <returns>The configuration properties of the resource</returns>
    ValueTask<Dictionary<string, object?>> GetConfigurationProperties();

    /// <summary>
    ///     Clears the resource configuration.
    /// </summary>
    Task ClearConfigurationAsync();

    /// <summary>
    ///     Gets the stored state of the resource.
    /// </summary>
    /// <returns>The stored state of the resource.</returns>
    ValueTask<ResourceStateStore> GetState();

    /// <summary>
    /// Gets the stored state properties of the resource.
    /// </summary>
    /// <returns>The stored state of the resource.</returns>
    ValueTask<Dictionary<string, object?>> GetStateProperties();

    /// <summary>
    ///     Clears the state from the resource store.
    /// </summary>
    Task ClearStateAsync();

    /// <summary>
    ///     Clears the resource from the persistence store.
    /// </summary>
    Task SelfRemoveAsync();
}