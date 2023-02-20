namespace Flownodes.Shared.Interfaces;

public interface ITenantGrain : IGrainWithStringKey
{
    Task UpdateMetadataAsync(Dictionary<string, string?> metadata);
    ValueTask<Dictionary<string, string?>> GetMetadata();
    Task ClearMetadataAsync();
    ValueTask<Dictionary<string, string?>> GetMetadataAsync();
}