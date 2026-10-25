namespace Engine.Infrastructure.Assets;

internal interface IAssetGrain : IGrainWithStringKey
{
    Task ApplyAsync(Guid uid, long generation, long seq, string spec);
    Task DeleteAsync(Guid uid, long seq);
    Task PingAsync();
    Task AssetTypeChangedAsync(AssetTypeInfo assetType);
}
