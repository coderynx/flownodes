namespace Flownodes.Sdk.Resourcing;

/// <summary>
///     The interface for implementing custom device behaviours with writable state.
/// </summary>
public interface IWritableDeviceBehaviour : IBehaviour
{
    /// <summary>
    ///     Code to execute when state push from device is requested.
    /// </summary>
    /// <param name="newState">New state to push to device.</param>
    /// <param name="context">The current context of the device.</param>
    /// <returns></returns>
    Task OnPushStateAsync(Dictionary<string, object?> newState, ResourceContext context);
}