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
    public ValueTask<IResourceSummary> GetSummary();

    /// <summary>
    ///     Updates the resource metadata.
    /// </summary>
    Task UpdateMetadataAsync(Dictionary<string, object?> metadata);

    /// <summary>
    ///     Gets the stored resource metadata.
    /// </summary>
    /// <returns>The stored metadata.</returns>
    ValueTask<Dictionary<string, object?>> GetMetadata();

    /// <summary>
    ///     Clears the resource metadata.
    /// </summary>
    Task ClearMetadataAsync();

    /// <summary>
    ///     Clears the resource from the persistence store.
    /// </summary>
    Task SelfRemoveAsync();
}