namespace Flownodes.Shared.Interfaces;

/// <summary>
///     The interface that defines a resource with state.
/// </summary>
public interface IStatefulResource : IResourceGrain
{
    /// <summary>
    ///     Updates the state of the resource by storing the new state and applying it.
    /// </summary>
    /// <param name="state">The new state of the resource to store.</param>
    Task UpdateStateAsync(Dictionary<string, object?> state);

    /// <summary>
    ///     Gets the stored state of the resource.
    /// </summary>
    /// <returns>The stored state of the resource.</returns>
    ValueTask<(Dictionary<string, object?> Properties, DateTime LastUpdate)> GetState();
}