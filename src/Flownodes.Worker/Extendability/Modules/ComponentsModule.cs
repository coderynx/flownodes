using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing.Attributes;
using Flownodes.Sdk.Resourcing.Behaviours;
using Module = Autofac.Module;

namespace Flownodes.Worker.Extendability.Modules;

public class ComponentsModule : Module
{
    private static string GetBehaviourName(MemberInfo type)
    {
        if (type.GetCustomAttribute(typeof(BehaviourIdAttribute)) is not BehaviourIdAttribute attribute)
            throw new InvalidOperationException("BehaviourIdAttribute not defined");

        return attribute.Id;
    }

    private static string GetAlerterDriverName(MemberInfo type)
    {
        if (type.GetCustomAttribute(typeof(AlerterDriverIdAttribute)) is not AlerterDriverIdAttribute attribute)
            throw new InvalidOperationException("AlerterDriverId not defined");

        return attribute.Id;
    }

    protected override void Load(ContainerBuilder builder)
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        var scannerPattern = new[] { "Flownodes.Components.*.dll" };
        var componentsPath = Path.Combine(Directory.GetCurrentDirectory(), "components");

        if (!Directory.Exists(componentsPath)) Directory.CreateDirectory(componentsPath);

        var assemblies = Directory
            .EnumerateFiles(componentsPath, "*.dll", SearchOption.AllDirectories)
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