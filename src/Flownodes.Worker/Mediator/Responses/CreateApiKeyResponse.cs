namespace Flownodes.Worker.Mediator.Responses;

public sealed record CreateApiKeyResponse : Response
{
    public CreateApiKeyResponse(string username, string apiKeyName, string apiKey)
    {
        Username = username;
        ApiKeyName = apiKeyName;
        ApiKey = apiKey;
    }

    public CreateApiKeyResponse(string username, string apiKeyName, string message, ResponseKind responseKind) : base(
        message, responseKind)
    {
        Username = username;
        ApiKeyName = apiKeyName;
    }

    public string Username { get; }
    public string ApiKeyName { get; }
    public string? ApiKey { get; }
}