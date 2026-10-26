using Identity.Application.Tokens;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Identity.Application.Bootstrap;

/// <summary>
/// Adds Identity's application layer to the host.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Identity's command and query handlers with the shared mediator, and the services they share.
    /// </summary>
    /// <param name="builder">The host Identity is being added to.</param>
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        Core.Application.Bootstrap.DependencyInjection.AddCqrs(builder, typeof(DependencyInjection).Assembly);

        builder.Services.TryAddScoped<ICallerChecks, CallerChecks>();
        builder.Services.TryAddScoped<IUserTokens, UserTokens>();
        builder.Services.TryAddScoped<IPasswordPolicy, PasswordPolicy>();
        builder.Services.TryAddSingleton<IPersonalAccessTokenSecrets, PersonalAccessTokenSecrets>();
    }
}
