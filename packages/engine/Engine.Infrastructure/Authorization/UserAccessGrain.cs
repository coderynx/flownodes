using Core.Domain.Authorization;
using Microsoft.Extensions.Logging;

namespace Engine.Infrastructure.Authorization;

internal sealed class UserAccessGrain(
    [PersistentState("user-access")] IPersistentState<UserAccessState> state,
    ILogger<UserAccessGrain> logger) : Grain, IUserAccessGrain
{
    public Task GrantRoleAsync(string @namespace, NamespaceRole role, long seq) =>
        ChangeAsync(
            seq,
            current =>
            {
                var grant = new GrantState(@namespace, role);
                if (!current.Grants.Contains(grant))
                {
                    current.Grants.Add(grant);
                }
            });

    public Task RevokeRoleAsync(string @namespace, NamespaceRole role, long seq) =>
        ChangeAsync(seq, current => current.Grants.Remove(new GrantState(@namespace, role)));

    public Task DisableAsync(long seq) => ChangeAsync(seq, current => current.Disabled = true);
    public Task EnableAsync(long seq) => ChangeAsync(seq, current => current.Disabled = false);

    public Task DeleteAsync(long seq) =>
        ChangeAsync(
            seq,
            current =>
            {
                current.Deleted = true;
                current.Grants.Clear();
            });

    public Task ReplaceAsync(bool disabled, bool deleted, IReadOnlyList<GrantState> grants, long seq) =>
        ChangeAsync(
            seq,
            current =>
            {
                current.Disabled = disabled;
                current.Deleted = deleted;
                current.Grants = deleted ? [] : [.. grants.Distinct()];
            });

    public Task<UserAccessSnapshot> GetAsync()
    {
        var current = state.State;
        var isKnown = current.Seq > 0;
        var isActive = isKnown && current is { Disabled: false, Deleted: false };

        return Task.FromResult(new UserAccessSnapshot(isActive, [.. current.Grants], isKnown));
    }

    private async Task ChangeAsync(long seq, Action<UserAccessState> change)
    {
        if (seq <= state.State.Seq)
        {
            logger.LogDebug(
                "Dropped message {Seq} for User {UserId}, not newer than {Applied}",
                seq,
                this.GetPrimaryKey(),
                state.State.Seq);

            return;
        }

        state.State.Seq = seq;
        change(state.State);
        await state.WriteStateAsync();
    }
}
