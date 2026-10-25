using System.Text.Json;
using Engine.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Assets;

[Reentrant]
internal sealed class AssetTypeGrain(
    [PersistentState("assettype")] IPersistentState<ResourceState> state,
    IStatusReporter reporter,
    IOptions<AssetTypeGrainOptions> options,
    ILogger<AssetTypeGrain> logger)
    : ResourceGrain(state, reporter, options.Value.StatusInterval, "AssetType"), IAssetTypeGrain
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly HashSet<string> _instances = [];

    public async Task ApplyAsync(Guid uid, long generation, long seq, string spec)
    {
        if (!await AcceptAsync(uid, generation, seq, spec))
        {
            return;
        }

        await NotifyInstancesAsync();
        await ReportStatusAsync();
    }

    public async Task DeleteAsync(Guid uid, long seq)
    {
        if (!await TombstoneAsync(uid, seq))
        {
            return;
        }

        await NotifyInstancesAsync();
        DeactivateOnIdle();
    }

    public Task PingAsync() => Task.CompletedTask;

    public async Task<AssetTypeInfo> RegisterInstanceAsync(string asset)
    {
        if (_instances.Add(asset))
        {
            await ReportStatusAsync();
        }

        return Info();
    }

    public async Task UnregisterInstanceAsync(string asset)
    {
        if (_instances.Remove(asset))
        {
            await ReportStatusAsync();
        }
    }

    protected override JsonElement BuildStatus() =>
        JsonSerializer.SerializeToElement(
            new
            {
                conditions = new[]
                    { new { type = "Ready", status = "True", reason = "Declared", message = (string?)null } },
                instances = _instances.Count
            },
            Web);

    private AssetTypeInfo Info() => new(IsLive, IsLive ? DataPointDeclarations.Names(Spec) : []);

    private async Task NotifyInstancesAsync()
    {
        var info = Info();
        foreach (var asset in _instances.ToList())
        {
            try
            {
                await GrainFactory.GetGrain<IAssetGrain>($"{Namespace}/{asset}").AssetTypeChangedAsync(info);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                logger.LogWarning(exception, "Asset {Name} could not be told its Asset Type changed", asset);
            }
        }
    }
}
