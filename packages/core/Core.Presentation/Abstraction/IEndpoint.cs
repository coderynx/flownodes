using Microsoft.AspNetCore.Routing;

namespace Core.Presentation.Abstraction;

/// <summary>
/// A self-contained Minimal API endpoint (or group of endpoints).
/// Implementations register their routes in <see cref="Map"/>.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Maps this endpoint's route(s) onto the given route builder.
    /// </summary>
    /// <param name="app">
    /// The route builder to register on. This is either the application itself
    /// or a <see cref="RouteGroupBuilder"/> that carries a shared prefix, filters, or auth policy.
    /// </param>
    void Map(IEndpointRouteBuilder app);
}
