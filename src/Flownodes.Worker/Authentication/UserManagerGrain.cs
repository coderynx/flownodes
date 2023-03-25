using Flownodes.Sdk.Entities;
using Flownodes.Shared.Authentication;
using Flownodes.Shared.Authentication.Models;
using Orleans.Runtime;

namespace Flownodes.Worker.Authentication;

[GrainType(FlownodesEntityNames.UserManager)]
public class UserManagerGrain : Grain, IUserManagerGrain
{
    private readonly ILogger<UserManagerGrain> _logger;
    private readonly IPersistentState<List<ApplicationUser>> _store;

    public UserManagerGrain(ILogger<UserManagerGrain> logger,
        [PersistentState("userManagerStore")] IPersistentState<List<ApplicationUser>> store)
    {
        _logger = logger;
        _store = store;
    }

    public async Task CreateUserAsync(ApplicationUser user)
    {
        _store.State.Add(user);
        await _store.WriteStateAsync();

        _logger.LogInformation("Added new user {@Username} to user manager store", user.UserName);
    }

    public async Task UpdateUserAsync(ApplicationUser user)
    {
        var index = _store.State.FindIndex(u => u.Id.Equals(user.Id));
        if (index < 0)
            throw new InvalidOperationException($"Could not find a user with the given ID {user.Id}");

        _store.State[index] = user;
        await _store.WriteStateAsync();

        _logger.LogInformation("Updated user {@Username}", user.UserName);
    }

    public async Task DeleteUserAsync(ApplicationUser user)
    {
        var index = _store.State.FindIndex(u => u.Id.Equals(user.Id));
        if (index < 0)
            throw new InvalidOperationException($"Could not find a user with the given ID {user.Id}");

        _store.State.RemoveAt(index);
        await _store.WriteStateAsync();

        _logger.LogInformation("Deleted user {@Username}", user.UserName);
    }

    public ValueTask<ApplicationUser?> FindByIdAsync(string userId)
    {
        return ValueTask.FromResult(_store.State.SingleOrDefault(u => u.Id.Equals(userId)));
    }

    public ValueTask<ApplicationUser?> FindByNormalizedUsernameAsync(string normalizedUsername)
    {
        var record = _store.State
            .SingleOrDefault(user =>
                user.NormalizedUserName is not null && user.NormalizedUserName.Equals(normalizedUsername));
        return ValueTask.FromResult(record);
    }

    public ValueTask<bool> HasUsers()
    {
        return ValueTask.FromResult(_store.State.Count > 0);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated user manager grain");
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated user manager grain for reason {@DeactivationReason}", reason.Description);
        return Task.CompletedTask;
    }
}