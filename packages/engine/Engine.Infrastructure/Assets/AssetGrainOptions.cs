namespace Engine.Infrastructure.Assets;

internal sealed class AssetGrainOptions
{
    public const string Section = "Flownodes:Engine:AssetGrain";
    public TimeSpan StatusInterval { get; set; } = TimeSpan.FromSeconds(1);
}
