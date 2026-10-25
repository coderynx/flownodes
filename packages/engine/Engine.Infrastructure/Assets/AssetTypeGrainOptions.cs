namespace Engine.Infrastructure.Assets;

internal sealed class AssetTypeGrainOptions
{
    public const string Section = "Flownodes:Engine:AssetTypeGrain";
    public TimeSpan StatusInterval { get; set; } = TimeSpan.FromSeconds(1);
}
