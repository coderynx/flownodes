using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Web;

internal sealed class ClientRouteConstraint : IRouteConstraint
{
    public const string Name = "clientroute";

    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        var path = new PathString($"/{values[routeKey]}");

        return !path.StartsWithSegments("/api") && !path.StartsWithSegments("/bff") &&
               !path.StartsWithSegments("/connect");
    }
}
