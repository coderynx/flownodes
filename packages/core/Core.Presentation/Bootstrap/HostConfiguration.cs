using Microsoft.AspNetCore.Builder;
using System.Reflection;
using Core.Presentation.Abstraction;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Core.Presentation.Bootstrap;

public static class HostConfiguration
{
    public static void UsePresentation(this WebApplication app)
    {
        var properties = ((IApplicationBuilder)app).Properties;
        if (!properties.TryAdd(PresentationMappedKey, true))
        {
            return;
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseApiDocumentation();
        }

        app.MapEndpoints(typeof(HostConfiguration).Assembly);
    }

    private const string PresentationMappedKey = "Core.Presentation.Mapped";

    /// <summary>
    /// Maps the OpenAPI document (<c>/openapi/v1.json</c>) and the Scalar API reference (<c>/scalar</c>).
    /// </summary>
    private static void UseApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options => options
            .WithTitle("Flownodes API")
            .WithTheme(ScalarTheme.Default));
    }
    
    /// <summary>
    /// Discovers every concrete <see cref="IEndpoint"/> in the given assemblies and maps it.
    /// Falls back to the entry assembly when none are provided.
    /// </summary>
    public static void MapEndpoints(this WebApplication app, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetEntryAssembly()!];
        }

        var endpoints = assemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => (IEndpoint)Activator.CreateInstance(type)!);

        foreach (var endpoint in endpoints)
        {
            endpoint.Map(app);
        }
    }
}
