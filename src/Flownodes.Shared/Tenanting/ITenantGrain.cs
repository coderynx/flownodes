using Flownodes.Shared.Alerting;
using Flownodes.Shared.Resourcing;

namespace Flownodes.Shared.Tenanting;

public interface ITenantGrain : IGrainWithStringKey
{
    Task UpdateMetadataAsync(Dictionary<string, string?> metadata);
    ValueTask<Dictionary<string, string?>> GetMetadata();
    Task ClearMetadataAsync();
    ValueTask<IResourceManagerGrain> GetResourceManager();
    ValueTask<IAlertManagerGrain> GetAlertManager();
}