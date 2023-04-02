using Flownodes.Shared.Entities;

namespace Flownodes.Shared.Resourcing.Grains;

/// <summary>
///     The interface that defines the base resource methods.
/// </summary>
public interface IResourceGrain : IEntityGrain
{
    /// <summary>
    ///     Gets the resource POCO.
    /// </summary>
    /// <returns>The resource POCO. </returns>
    public ValueTask<ResourceSummary> GetSummary();

    /// <summary>
    ///     Updates the resource metadata.
    /// </summary>
    Task UpdateMetadataAsync(Dictionary<string, string?> metadata);

    /// <summary>
    ///     Gets the stored resource metadata.
    /// </summary>
    /// <returns>The stored metadata.</returns>
    ValueTask<(Dictionary<string, string?> Metadata, DateTime CreatedAtDate)> GetMetadata();

    /// <summary>
    ///     Clears the resource metadata.
    /// </summary>
    Task ClearMetadataAsync();

    /// <summary>
    ///     Clears the resource from the persistence store.
    /// </summary>
    Task SelfRemoveAsync();

    /// <summary>
    ///     Checks if the resource is configurable.
    /// </summary>
    /// <returns>True if it's configurable, otherwise false.</returns>
    ValueTask<bool> GetIsConfigurable();

    /// <summary>
    ///     Checks if the resource is stateful.
    /// </summary>
    /// <returns>True if it's stateful, otherwise false.</returns>
    ValueTask<bool> GetIsStateful();
}