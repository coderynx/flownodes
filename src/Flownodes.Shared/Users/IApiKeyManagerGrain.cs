namespace Flownodes.Shared.Users;

public interface IApiKeyManagerGrain : IGrainWithStringKey
{
    ValueTask<string> GenerateApiKeyAsync(string name, string username);
    Task DeleteApiKeyAsync(string name, string username);
    ValueTask<bool> IsApiKeyValid(string apiKey);
}