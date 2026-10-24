using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;

namespace Core.Presentation.Bootstrap;

/// <summary>
/// Registers the presentation services every module shares.
/// </summary>
public static class DependencyInjection
{
    private const string AppliedKey = "Core.Presentation.Applied";

    /// <summary>
    /// The setting holding Identity's issuer URL, which resource servers fetch discovery and signing keys from.
    /// </summary>
    public const string AuthoritySetting = "Flownodes:Identity:Authority";

    private const string TokenValidationAppliedKey = "Core.Presentation.TokenValidation.Applied";

    /// <summary>
    /// Registers the OpenAPI document generator behind the Scalar API reference and the authorisation fallback policy
    /// that requires an authenticated caller on every endpoint that does not say otherwise.
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

        builder.Services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        builder.Services.AddOpenApi(options => options.CreateSchemaReferenceId = type =>
            type.Type.DeclaringType is { } declaringType
                ? $"{declaringType.Name}.{type.Type.Name}"
                : OpenApiOptions.CreateDefaultSchemaReferenceId(type));
    }

    /// <summary>
    /// Authenticates callers by the access tokens Identity issues, validated locally with the signing keys fetched
    /// over HTTP from Identity's OIDC discovery document (ADR-0026).
    /// </summary>
    /// <remarks>
    /// Every module's Presentation that needs tokens calls this, so it registers once per host and ignores later
    /// calls. The <c>Flownodes:Identity:Authority</c> setting is Identity's issuer URL.
    /// </remarks>
    /// <param name="builder">The host the module is being added to.</param>
    /// <exception cref="InvalidOperationException">
    /// The <c>Flownodes:Identity:Authority</c> setting is missing.
    /// </exception>
    public static void AddTokenValidation(this IHostApplicationBuilder builder)
    {
        if (!builder.Properties.TryAdd(TokenValidationAppliedKey, true))
        {
            return;
        }

        var authority = builder.Configuration[AuthoritySetting] ??
                        throw new InvalidOperationException($"Setting '{AuthoritySetting}' not found.");

        builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        builder.Services.AddOpenIddict()
            .AddValidation(validation =>
            {
                validation.SetIssuer(new Uri(authority));
                validation.UseSystemNetHttp();
                validation.UseAspNetCore();
            });
    }
}
