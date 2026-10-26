using System.Reflection;
using Coderynx.MediatorKit;
using Core.Application.Authorization;
using Core.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Core.Application.Bootstrap;

/// <summary>
/// Registers the application plumbing every module shares: the mediator its commands and queries run through.
/// </summary>
public static class DependencyInjection
{
    private const string AppliedKey = "Core.Application.Applied";

    /// <summary>
    /// Registers the mediator, the shared pipeline behaviors and the request handlers found in the given assemblies.
    /// </summary>
    /// <remarks>
    /// Every module calls this with its own Application assembly. The pipeline behaviors are added only by the first
    /// call, so a request is logged once however many modules the host composes.
    /// </remarks>
    /// <param name="builder">The host the module is being added to.</param>
    /// <param name="handlerAssemblies">The assemblies whose request handlers are registered.</param>
    public static void AddCqrs(this IHostApplicationBuilder builder, params Assembly[] handlerAssemblies)
    {
        var first = builder.Properties.TryAdd(AppliedKey, true);

        builder.Services.AddMediatorKit(mediator =>
        {
            if (first)
            {
                mediator.AddRequestPipelineBehavior(typeof(LoggingBehavior<,>));
            }

            foreach (var assembly in handlerAssemblies)
            {
                mediator.AddRequestHandlersFromAssembly(assembly);
            }
        });
    }

    /// <summary>
    /// Registers the authorisation checks and the projection of Identity's messages, which run against the replica of
    /// Role Grants in the module's database.
    /// </summary>
    /// <remarks>
    /// The host has one set of checks, so only one module registers them.
    /// </remarks>
    /// <typeparam name="TSource">The module's database, which holds its replica of Role Grants.</typeparam>
    /// <param name="builder">The host the module is being added to.</param>
    public static void AddAccessChecks<TSource>(this IHostApplicationBuilder builder)
        where TSource : class, IUserAccessSource
    {
        builder.Services.TryAddScoped<IAccessChecks>(services => new AccessChecks(
            services.GetRequiredService<TSource>()));
        builder.Services.TryAddScoped<IUserAccessProjection>(services => new UserAccessProjection(
            services.GetRequiredService<TSource>()));
    }
}
