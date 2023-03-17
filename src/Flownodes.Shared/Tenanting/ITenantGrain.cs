namespace Flownodes.Shared.Tenanting;

public interface ITenantGrain : IGrainWithStringKey
{
    Task UpdateMetadataAsync(Dictionary<string, string?> metadata);
    ValueTask<Dictionary<string, string?>> GetMetadata();
    Task ClearMetadataAsync();
}