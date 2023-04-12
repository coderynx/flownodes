namespace Flownodes.Shared.Resourcing.Grains;

/// <summary>
///     The device grain is a resource that represents the "digital twin" of a specific device.
/// </summary>
public interface IDeviceGrain : IConfigurableResourceGrain, IStatefulResourceGrain
{
    /// <summary>
    ///     Updates the BehaviourId.
    /// </summary>
    /// <param name="behaviourId">The BehaviourId to set.</param>
    Task UpdateBehaviourId(string behaviourId);
}