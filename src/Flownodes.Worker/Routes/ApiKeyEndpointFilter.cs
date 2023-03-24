using Flownodes.Sdk.Entities;
using Flownodes.Shared.Users;

namespace Flownodes.Worker.Routes;

public class ApiKeyEndpointFilter : IEndpointFilter
{
    private readonly IApiKeyManagerGrain _apiKeyManager;

    public ApiKeyEndpointFilter(IGrainFactory grainFactory)
    {
        _apiKeyManager = grainFactory.GetGrain<IApiKeyManagerGrain>(FlownodesEntityNames.ApiKeyManager);
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var receivedApiKey))
            return TypedResults.Unauthorized();

        if (!await _apiKeyManager.IsApiKeyValid(receivedApiKey.ToString())) return TypedResults.Unauthorized();

        return await next(context);
    }
}