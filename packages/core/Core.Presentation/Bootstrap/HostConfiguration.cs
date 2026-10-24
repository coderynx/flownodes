using System.Reflection;
using Core.Presentation.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Core.Presentation.Bootstrap;

/// <summary>
/// Maps the HTTP surface every module shares and the endpoints each module declares.
/// </summary>
public static class HostConfiguration
{
    /// <summary>
    /// The prefix every module's endpoints hang off. A module maps its own path below it, never the version.
    /// </summary>
    public const string ApiPrefix = "/api/v1";

    private const string PresentationMappedKey = "Core.Presentation.Mapped";

    /// <summary>
    /// Adds authentication and authorisation to the pipeline, and maps the OpenAPI document
    /// (<c>/openapi/v1.json</c>) and the Scalar API reference (<c>/scalar</c>) in Development.
    /// </summary>
    /// <remarks>
    /// Every module's Presentation calls this, so it maps once per host and ignores later calls.
    /// </remarks>
    /// <param name="app">The host the module is being added to.</param>
    public static void UsePresentation(this WebApplication app)
    {
        var properties = ((IApplicationBuilder)app).Properties;
        if (!properties.TryAdd(PresentationMappedKey, true))
        {
            return;
        }

        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference(options => options.WithTitle("Flownodes API").WithTheme(ScalarTheme.Default))
                .AllowAnonymous();
        }
    }

    /// <summary>
    /// Discovers every concrete <see cref="IEndpoint"/> in the given assemblies and maps it under
    /// <see cref="ApiPrefix"/>.
    /// </summary>
    /// <param name="app">The host the endpoints are mapped on.</param>
    /// <param name="assemblies">The assemblies to search; the entry assembly when none are given.</param>
    public static void MapEndpoints(this WebApplication app, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetEntryAssembly()!];
        }

        var api = app.MapGroup(ApiPrefix);
        var endpoints = assemblies.Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => (IEndpoint)Activator.CreateInstance(type)!);

        foreach (var endpoint in endpoints)
        {
            endpoint.Map(api);
        }
    }
}
