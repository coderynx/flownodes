namespace Flownodes.Shared.Resourcing.Grains;

public interface IAssetGrain : IResourceGrain
{
    ValueTask<Dictionary<string, object?>> GetProperties();
    Task UpdatePropertiesAsync(Dictionary<string, object?> properties);
    Task ClearPropertiesAsync();
}