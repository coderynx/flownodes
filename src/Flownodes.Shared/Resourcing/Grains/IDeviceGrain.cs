namespace Flownodes.Shared.Resourcing.Grains;

/// <summary>
///     The device grain is a resource that represents the "digital twin" of a specific device.
/// </summary>
public interface IDeviceGrain : IConfigurableResourceGrain, IStatefulResourceGrain
{
}