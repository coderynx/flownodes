using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

/// <summary>
///     The interface that defines the base resource methods.
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
    ///     Updates the state of the resource by storing the new state and applying it.
    /// </summary>
    /// <param name="metadata">The new state of the resource to store.</param>
    Task UpdateMetadataAsync(Dictionary<string, string?> metadata);

    /// <summary>
    ///     Gets the stored resource metadata.
    /// </summary>
    /// <returns>The stored metadata.</returns>
    ValueTask<(Dictionary<string, string?> Proprties, DateTime CreatedAt)> GetMetadata();

    /// <summary>
    ///     Clears the resource metadata.
    /// </summary>
    /// <returns></returns>
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