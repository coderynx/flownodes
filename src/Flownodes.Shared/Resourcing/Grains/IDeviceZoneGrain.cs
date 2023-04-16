using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Resourcing.Grains;

public interface IDeviceZoneGrain : IResourceGrain
{
    Task RegisterDeviceAsync(EntityId id);
    Task UnregisterDeviceAsync(EntityId id);
    ValueTask<IDeviceGrain?> GetDeviceAsync(EntityId id);
    ValueTask<HashSet<string>> GetRegistrations();
    Task ClearRegistrationsAsync();
}