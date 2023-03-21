namespace Flownodes.Sdk.Resourcing.Behaviours;

/// <summary>
///     The interface for implementing custom device behaviours with readable state.
/// </summary>
public interface IReadableDeviceBehaviour : IBehaviour
{
    /// <summary>
    ///     Code to execute when state state pull from device is requested.
    /// </summary>
    /// <param name="context">The current context of the device.</param>
    /// <returns></returns>
    Task OnPullStateAsync(ResourceContext context);
}