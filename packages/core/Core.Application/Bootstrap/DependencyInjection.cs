using System.Reflection;
using Coderynx.MediatorKit;
using Core.Application.Behaviors;
using Microsoft.Extensions.Hosting;

namespace Core.Application.Bootstrap;

public static class DependencyInjection
{
    private const string AppliedKey = "Core.Application.Applied";

    /// <summary>
    /// Registers the mediator, the shared pipeline behaviors and the request handlers found in the given assemblies.
    /// Safe to call once per module.
    /// </summary>
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
}
