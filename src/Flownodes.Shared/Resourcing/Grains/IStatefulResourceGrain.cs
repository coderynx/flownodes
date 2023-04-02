namespace Flownodes.Shared.Resourcing.Grains;

/// <summary>
///     The interface that defines a resource with state.
/// </summary>
public interface IStatefulResourceGrain : IResourceGrain
{
    /// <summary>
    ///     Updates the state of the resource by storing the new state and applying it.
    /// </summary>
    /// <param name="state">The state to store.</param>
    Task UpdateStateAsync(Dictionary<string, object?> state);

    /// <summary>
    ///     Gets the currently stored state of the resource.
    /// </summary>
    /// <returns>The stored state of the resource.</returns>
    ValueTask<(Dictionary<string, object?> State, DateTime? LastUpdateDate)> GetState();

    /// <summary>
    ///     Clears the resource state.
    /// </summary>
    Task ClearStateAsync();
}