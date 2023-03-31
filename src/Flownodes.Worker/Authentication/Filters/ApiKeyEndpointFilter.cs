using Flownodes.Shared.Authentication;
using Flownodes.Worker.Services;
using Microsoft.Extensions.Options;

namespace Flownodes.Worker.Authentication.Filters;

public class ApiKeyEndpointFilter : IEndpointFilter
{
    private readonly AdminSecret _adminSecret;
    private readonly IApiKeyManagerGrain _apiKeyManager;

    public ApiKeyEndpointFilter(IEnvironmentService environmentService, IOptions<AdminSecret> adminSecret)
    {
        _apiKeyManager = environmentService.GetApiKeyManager();
        _adminSecret = adminSecret.Value;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        const string apiKeyHeaderName = "X-Api-Key";

        if (!context.HttpContext.Request.Headers.TryGetValue(apiKeyHeaderName, out var receivedApiKey))
            return TypedResults.Unauthorized();

        if (_adminSecret.Secret.Equals(receivedApiKey.ToString())) return await next(context);

        if (!await _apiKeyManager.IsApiKeyValid(receivedApiKey.ToString())) return TypedResults.Unauthorized();

        return await next(context);
    }
}