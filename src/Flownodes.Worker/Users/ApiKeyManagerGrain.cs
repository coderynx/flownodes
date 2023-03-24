using System.Security.Cryptography;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Users;
using Orleans.Runtime;

namespace Flownodes.Worker.Users;

internal sealed record ApiKey(string Name, string Username, string Value);

[GrainType(FlownodesEntityNames.ApiKeyManager)]
internal sealed class ApiKeyManagerGrain : Grain, IApiKeyManagerGrain
{
    private readonly ILogger<ApiKeyManagerGrain> _logger;
    private readonly IPersistentState<HashSet<ApiKey>> _store;

    public ApiKeyManagerGrain(ILogger<ApiKeyManagerGrain> logger,
        [PersistentState("apiKeyManagerStore")]
        IPersistentState<HashSet<ApiKey>> store)
    {
        _logger = logger;
        _store = store;
    }

    public async ValueTask<string> GenerateApiKeyAsync(string name, string username)
    {
        var apiKey = GenerateApiKey();

        _store.State.Add(new ApiKey(name, username, apiKey));
        await _store.WriteStateAsync();

        _logger.LogInformation("Generated ApiKey {@ApiKeyName} for user {Username}", name, username);

        return apiKey;
    }

    public async Task DeleteApiKeyAsync(string name, string username)
    {
        _store.State.RemoveWhere(x => x.Username.Equals(username) && x.Name.Equals(name));
        await _store.WriteStateAsync();

        _logger.LogInformation("Removed ApiKey {@ApiKeyName} of user {@Username}", name, username);
    }

    public ValueTask<bool> IsApiKeyValid(string apiKey)
    {
        return ValueTask.FromResult(_store.State.Any(x => x.Value.Equals(apiKey)));
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated ApiKeyManagerGrain");
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated ApiKeyManagerGrain for reason {@DeactivationReason}", reason.Description);
        return Task.CompletedTask;
    }

    private static string GenerateApiKey()
    {
        const int lenght = 32;
        var bytes = RandomNumberGenerator.GetBytes(lenght);

        var base64String = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_");

        var keyLength = lenght - "FN-".Length;

        return "FN-" + base64String[..keyLength];
    }
}