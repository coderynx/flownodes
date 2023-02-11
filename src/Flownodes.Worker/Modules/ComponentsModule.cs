using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing;
using Module = Autofac.Module;

namespace Flownodes.Worker.Modules;

public class ComponentsModule : Module
{
    private static string GetBehaviourName(MemberInfo type)
    {
        var attribute = type.GetCustomAttribute(typeof(BehaviourIdAttribute)) as BehaviourIdAttribute;
        ArgumentNullException.ThrowIfNull(attribute);
        return attribute.Id;
    }

    private static string GetAlerterDriverName(MemberInfo type)
    {
        var attribute = type.GetCustomAttribute(typeof(AlerterDriverIdAttribute)) as AlerterDriverIdAttribute;
        ArgumentNullException.ThrowIfNull(attribute);
        return attribute.Id;
    }

    protected override void Load(ContainerBuilder builder)
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        var scannerPattern = new[] { "Flownodes.Components.*.dll" };
        var componentsPath = Path.Combine(Directory.GetCurrentDirectory(), "components");

        if (!Directory.Exists(componentsPath)) Directory.CreateDirectory(componentsPath);

        var assemblies = Directory.EnumerateFiles(componentsPath, "*.dll", SearchOption.AllDirectories)
            .Where(filename => scannerPattern.Any(pattern => Regex.IsMatch(filename, pattern)))
            .Select(Assembly.LoadFrom)
            .ToArray();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(x => typeof(IBehaviour).IsAssignableFrom(x))
            .As<IBehaviour>()
            .Keyed<IBehaviour>(x => GetBehaviourName(x));

        builder.RegisterAssemblyTypes(assemblies)
            .Where(x => typeof(IAlerterDriver).IsAssignableFrom(x))
            .As<IAlerterDriver>()
            .Keyed<IAlerterDriver>(x => GetAlerterDriverName(x));
    }
}