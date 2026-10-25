namespace Engine.Infrastructure.Assets;

internal interface IAssetTypeGrain : IGrainWithStringKey
{
    Task ApplyAsync(Guid uid, long generation, long seq, string spec);
    Task DeleteAsync(Guid uid, long seq);
    Task PingAsync();
    Task<AssetTypeInfo> RegisterInstanceAsync(string asset);
    Task UnregisterInstanceAsync(string asset);
}
