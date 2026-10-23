using Microsoft.AspNetCore.Routing;

namespace Core.Presentation.Abstraction;

/// <summary>
/// One HTTP use case's Minimal API endpoint, discovered and mapped by its module's Presentation.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Maps this endpoint's route onto the given route builder.
    /// </summary>
    /// <param name="app">
    /// The versioned API group (<c>/api/v1</c>). The endpoint maps its module's path below it, never the version.
    /// </param>
    void Map(IEndpointRouteBuilder app);
}
