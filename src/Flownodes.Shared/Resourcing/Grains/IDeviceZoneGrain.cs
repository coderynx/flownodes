using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Resourcing.Grains;

public interface IDeviceZoneGrain : IResourceGrain
{
    Task RegisterDeviceAsync(FlownodesId id);
    Task UnregisterDeviceAsync(FlownodesId id);
    ValueTask<IDeviceGrain?> GetDeviceAsync(FlownodesId id);
    ValueTask<HashSet<string>> GetRegistrations();
    Task ClearRegistrationsAsync();
}