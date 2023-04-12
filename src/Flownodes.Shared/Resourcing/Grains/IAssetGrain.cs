namespace Flownodes.Shared.Resourcing.Grains;

public interface IAssetGrain : IResourceGrain
{
    ValueTask<Dictionary<string, object?>> GetState();
    Task UpdateStateAsync(Dictionary<string, object?> state);
    Task ClearStateAsync();
}