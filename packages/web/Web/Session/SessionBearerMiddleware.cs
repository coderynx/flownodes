using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Web.Session;

internal sealed class SessionBearerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        if (WantsSessionToken(request) && MayCarryToken(request))
        {
            var session = await context.AuthenticateAsync(SessionNames.SessionScheme);
            if (session.Succeeded && session.Properties.GetTokenValue(SessionNames.AccessToken) is { } token)
            {
                request.Headers.Authorization = $"Bearer {token}";
            }
        }

        await next(context);
    }

    private static bool WantsSessionToken(HttpRequest request) =>
        !request.Headers.ContainsKey(HeaderNames.Authorization) && request.Cookies.ContainsKey(SessionNames.Cookie);

    private static bool MayCarryToken(HttpRequest request) =>
        !(HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) ||
          HttpMethods.IsPatch(request.Method) || HttpMethods.IsDelete(request.Method)) ||
        request.Headers.ContainsKey(SessionNames.ClientHeader);
}
