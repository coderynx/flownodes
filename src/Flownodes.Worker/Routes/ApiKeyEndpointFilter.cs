using Flownodes.Shared.Authentication;
using Flownodes.Worker.Services;

namespace Flownodes.Worker.Routes;

public class ApiKeyEndpointFilter : IEndpointFilter
{
    private readonly IApiKeyManagerGrain _apiKeyManager;

    public ApiKeyEndpointFilter(IEnvironmentService environmentService)
    {
        _apiKeyManager = environmentService.GetApiKeyManager();
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        const string apiKeyHeaderName = "X-Api-Key";

        if (!context.HttpContext.Request.Headers.TryGetValue(apiKeyHeaderName, out var receivedApiKey))
            return TypedResults.Unauthorized();

        if (!await _apiKeyManager.IsApiKeyValid(receivedApiKey.ToString())) return TypedResults.Unauthorized();

        return await next(context);
    }
}