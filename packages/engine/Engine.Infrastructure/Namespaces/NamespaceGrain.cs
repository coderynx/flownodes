using Engine.Infrastructure.Assets;
using Engine.Infrastructure.Connections;
using Engine.Infrastructure.Devices;
using Engine.Infrastructure.Flows;
using Microsoft.Extensions.Options;

namespace Engine.Infrastructure.Namespaces;

internal sealed class NamespaceGrain(
    [PersistentState("namespace")] IPersistentState<NamespaceState> state,
    IRegistryRequests registry,
    IOptions<NamespaceGrainOptions> options) : Grain, INamespaceGrain, IRemindable
{
    private const string ReminderName = "keep-alive";
    private const string ConnectionKind = "Connection";
    private const string AssetTypeKind = "AssetType";
    private const string AssetKind = "Asset";
    private const string FlowKind = "Flow";
    private IDisposable? _resyncTimer;
    private IDisposable? _pingTimer;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (!state.State.Deleted)
        {
            StartResyncTimer();
            StartPingTimer();
            await registry.ResyncNamespaceAsync(this.GetPrimaryKeyString(), cancellationToken);
        }
    }

    public async Task ApplyAsync(Guid uid, long seq)
    {
        var current = state.State;
        if (seq <= current.Seq)
        {
            return;
        }

        var replaced = IsLive && current.Uid != uid;
        if (replaced)
        {
            foreach (var member in current.Members.OrderByDescending(member => PingOrder(member.Kind)).ToList())
            {
                await DeleteMemberAsync(member, Guid.Empty, current.Seq + 1);
            }
        }

        state.State = new NamespaceState
        {
            Uid = uid,
            Seq = seq,
            Members = replaced ? [] : current.Members
        };
        await state.WriteStateAsync();

        await this.RegisterOrUpdateReminder(ReminderName, options.Value.ReminderPeriod, options.Value.ReminderPeriod);
        if (current.Deleted)
        {
            StartResyncTimer();
            StartPingTimer();
        }

        if (replaced || seq > current.Seq + 1)
        {
            await registry.ResyncNamespaceAsync(this.GetPrimaryKeyString(), CancellationToken.None);
        }
    }

    public async Task DeleteAsync(Guid uid, long seq)
    {
        if (state.State.Deleted || seq <= state.State.Seq)
        {
            return;
        }

        state.State = new NamespaceState
        {
            Uid = uid,
            Seq = seq,
            Deleted = true
        };
        await state.WriteStateAsync();

        _resyncTimer?.Dispose();
        _resyncTimer = null;
        _pingTimer?.Dispose();
        _pingTimer = null;

        var reminder = await this.GetReminder(ReminderName);
        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder);
        }

        DeactivateOnIdle();
    }

    public async Task ObserveAsync(long seq)
    {
        var current = state.State;
        var isLive = !current.Deleted && current.Uid != Guid.Empty;
        if (!isLive || seq <= current.Seq)
        {
            return;
        }

        state.State = new NamespaceState
        {
            Uid = current.Uid,
            Seq = seq,
            Members = current.Members
        };
        await state.WriteStateAsync();

        if (seq > current.Seq + 1)
        {
            await registry.ResyncNamespaceAsync(this.GetPrimaryKeyString(), CancellationToken.None);
        }
    }

    public async Task ResourceAppliedAsync(string kind, string name, Guid uid, long generation, long seq, string spec)
    {
        if (!IsLive)
        {
            return;
        }

        await ObserveAsync(seq);
        var member = new Member(kind, name);
        if (!state.State.Members.Contains(member))
        {
            state.State.Members.Add(member);
            await state.WriteStateAsync();
        }

        switch (kind)
        {
            case ConnectionKind:
                await Connection(name).ApplyAsync(uid, generation, seq, spec);
                break;
            case AssetTypeKind:
                await AssetType(name).ApplyAsync(uid, generation, seq, spec);
                break;
            case AssetKind:
                await Asset(name).ApplyAsync(uid, generation, seq, spec);
                break;
            case FlowKind:
                await Flow(name).ApplyAsync(uid, generation, seq, spec);
                break;
            default:
                await Device(name).ApplyAsync(uid, generation, seq, spec);
                break;
        }
    }

    public async Task ResourceDeletedAsync(string kind, string name, Guid uid, long seq)
    {
        if (!IsLive)
        {
            return;
        }

        await ObserveAsync(seq);
        var member = new Member(kind, name);
        if (state.State.Members.Remove(member))
        {
            await state.WriteStateAsync();
        }

        await DeleteMemberAsync(member, uid, seq);
    }

    public Task WakeAsync() =>
        IsLive ? Task.CompletedTask : registry.ResyncNamespaceAsync(this.GetPrimaryKeyString(), CancellationToken.None);

    public Task ReceiveReminder(string reminderName, TickStatus status) => Task.CompletedTask;

    private void StartResyncTimer() =>
        _resyncTimer ??= this.RegisterGrainTimer(
            token => registry.ResyncNamespaceAsync(this.GetPrimaryKeyString(), token),
            options.Value.ResyncPeriod,
            options.Value.ResyncPeriod);

    private bool IsLive => !state.State.Deleted && state.State.Uid != Guid.Empty;

    private IConnectionGrain Connection(string name) =>
        GrainFactory.GetGrain<IConnectionGrain>($"{this.GetPrimaryKeyString()}/{name}");

    private IAssetTypeGrain AssetType(string name) =>
        GrainFactory.GetGrain<IAssetTypeGrain>($"{this.GetPrimaryKeyString()}/{name}");

    private IAssetGrain Asset(string name) =>
        GrainFactory.GetGrain<IAssetGrain>($"{this.GetPrimaryKeyString()}/{name}");

    private IFlowGrain Flow(string name) =>
        GrainFactory.GetGrain<IFlowGrain>($"{this.GetPrimaryKeyString()}/{name}");

    private IDeviceGrain Device(string name) =>
        GrainFactory.GetGrain<IDeviceGrain>($"{this.GetPrimaryKeyString()}/{name}");

    private Task DeleteMemberAsync(Member member, Guid uid, long seq) =>
        member.Kind switch
        {
            ConnectionKind => Connection(member.Name).DeleteAsync(uid, seq),
            AssetTypeKind => AssetType(member.Name).DeleteAsync(uid, seq),
            AssetKind => Asset(member.Name).DeleteAsync(uid, seq),
            FlowKind => Flow(member.Name).DeleteAsync(uid, seq),
            _ => Device(member.Name).DeleteAsync(uid, seq)
        };

    private void StartPingTimer() =>
        _pingTimer ??= this.RegisterGrainTimer(PingMembersAsync, options.Value.PingPeriod, options.Value.PingPeriod);

    private async Task PingMembersAsync()
    {
        foreach (var member in state.State.Members.OrderBy(member => PingOrder(member.Kind)).ToList())
        {
            try
            {
                switch (member.Kind)
                {
                    case ConnectionKind:
                        await Connection(member.Name).PingAsync();
                        break;
                    case AssetTypeKind:
                        await AssetType(member.Name).PingAsync();
                        break;
                    case AssetKind:
                        await Asset(member.Name).PingAsync();
                        break;
                    case FlowKind:
                        await Flow(member.Name).PingAsync();
                        break;
                    default:
                        await Device(member.Name).PingAsync();
                        break;
                }
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _ = exception;
            }
        }
    }

    private static int PingOrder(string kind) =>
        kind switch
        {
            ConnectionKind or AssetTypeKind => 0,
            FlowKind => 2,
            _ => 1
        };
}
