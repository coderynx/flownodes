using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Entities;
using Flownodes.Shared.Resourcing.Grains;

namespace Flownodes.Shared.Tenanting.Grains;

public interface ITenantGrain : IEntityGrain
{
    Task UpdateMetadataAsync(Dictionary<string, string?> metadata);
    ValueTask<Dictionary<string, string?>> GetMetadata();
    Task ClearMetadataAsync();
    ValueTask<IResourceManagerGrain> GetResourceManager();
    ValueTask<IAlertManagerGrain> GetAlertManager();
}