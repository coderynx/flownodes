using System.Text.Json;
using Engine.Infrastructure.Resources;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace Engine.Infrastructure.Assets;

[Reentrant]
internal sealed class AssetGrain(
    [PersistentState("asset")] IPersistentState<ResourceState> state,
    IStatusReporter reporter,
    IOptions<AssetGrainOptions> options)
    : ResourceGrain(state, reporter, options.Value.StatusInterval, "Asset"), IAssetGrain
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private AssetTypeInfo? _assetType;
    private string? _registeredWith;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (IsLive)
        {
            await RegisterAsync();
        }
    }

    public async Task ApplyAsync(Guid uid, long generation, long seq, string spec)
    {
        var previousType = _registeredWith;
        if (!await AcceptAsync(uid, generation, seq, spec))
        {
            return;
        }

        if (previousType is not null && previousType != AssetTypeName)
        {
            await AssetType(previousType).UnregisterInstanceAsync(ResourceName);
        }

        await RegisterAsync();
    }

    public async Task DeleteAsync(Guid uid, long seq)
    {
        var assetType = _registeredWith;
        if (!await TombstoneAsync(uid, seq))
        {
            return;
        }

        if (assetType is not null)
        {
            await AssetType(assetType).UnregisterInstanceAsync(ResourceName);
        }

        DeactivateOnIdle();
    }

    public async Task PingAsync()
    {
        if (IsLive)
        {
            await RegisterAsync();
        }
    }

    public async Task AssetTypeChangedAsync(AssetTypeInfo assetType)
    {
        if (!IsLive)
        {
            return;
        }

        _assetType = assetType;
        await ReportStatusAsync();
    }

    protected override JsonElement BuildStatus()
    {
        var (reason, ready, message) = Condition();

        return JsonSerializer.SerializeToElement(
            new
            {
                conditions = new[] { new { type = "Ready", status = ready ? "True" : "False", reason, message } },
                dataPoints = ready ? MergedDataPoints() : []
            },
            Web);
    }

    private string[] MergedDataPoints() =>
        [.. (_assetType?.DataPoints ?? []), .. DataPointDeclarations.Names(Spec)];

    private string? AssetTypeName =>
        Spec.TryGetProperty("assetType", out var name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;

    private IAssetTypeGrain AssetType(string name) =>
        GrainFactory.GetGrain<IAssetTypeGrain>($"{Namespace}/{name}");

    private (string Reason, bool Ready, string? Message) Condition()
    {
        if (AssetTypeName is not { } name)
        {
            return ("Resolved", true, null);
        }

        return _assetType is { Exists: true }
            ? ("Resolved", true, null)
            : ("AssetTypeMissing", false, $"Asset Type '{name}' is not there.");
    }

    private async Task RegisterAsync()
    {
        _registeredWith = AssetTypeName;
        _assetType = AssetTypeName is { } name ? await AssetType(name).RegisterInstanceAsync(ResourceName) : null;
        await ReportStatusAsync();
    }
}
