using Microsoft.Extensions.Options;

namespace Flownodes.Worker.Authentication.Filters;

public class ApiKeyAdminEndpointFilter : IEndpointFilter
{
    private readonly AdminSecret _adminSecret;

    public ApiKeyAdminEndpointFilter(IOptions<AdminSecret> adminSecret)
    {
        _adminSecret = adminSecret.Value;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        const string apiKeyHeaderName = "X-Api-Key";

        if (!context.HttpContext.Request.Headers.TryGetValue(apiKeyHeaderName, out var receivedApiKey))
            return TypedResults.Unauthorized();

        if (_adminSecret.Secret.Equals(receivedApiKey.ToString())) return await next(context);

        return await next(context);
    }
}