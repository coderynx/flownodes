using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Presentation.Bootstrap;

/// <summary>
/// Registers the presentation services every module shares.
/// </summary>
public static class DependencyInjection
{
    private const string AppliedKey = "Core.Presentation.Applied";

    /// <summary>
    /// Registers the OpenAPI document generator behind the Scalar API reference.
    /// </summary>
    /// <remarks>
    /// Every module's Presentation calls this, so it registers once per host and ignores later calls. A schema
    /// nested in another type is named after both, so two endpoints' <c>Response</c> records stay two schemas
    /// (ADR-0020).
    /// </remarks>
    /// <param name="builder">The host the module is being added to.</param>
    public static void AddPresentation(this IHostApplicationBuilder builder)
    {
        if (!builder.Properties.TryAdd(AppliedKey, true))
        {
            return;
        }

        builder.Services.AddOpenApi(options => options.CreateSchemaReferenceId = type =>
            type.Type.DeclaringType is { } declaringType
                ? $"{declaringType.Name}.{type.Type.Name}"
                : OpenApiOptions.CreateDefaultSchemaReferenceId(type));
    }
}
