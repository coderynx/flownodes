using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;

namespace Identity.Presentation.Bootstrap;

/// <summary>
/// Adds Identity's HTTP surface to the host.
/// </summary>
public static class DependencyInjection
{
    internal const string SessionScheme = "Flownodes.Session";

    /// <summary>
    /// Registers the sign-in page, the cookie it signs a person in with, and access token authentication for the API.
    /// </summary>
    /// <param name="builder">The host Identity is being added to.</param>
    public static void AddPresentation(this IHostApplicationBuilder builder)
    {
        Core.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);

        builder.Services.AddRazorPages().AddApplicationPart(typeof(DependencyInjection).Assembly);
        builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .AddCookie(
                SessionScheme,
                options =>
                {
                    options.LoginPath = "/signin";
                    options.Cookie.Name = "flownodes.session";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                });
        builder.Services.AddAuthorization();
    }
}
