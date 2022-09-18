using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Flownodes.Cluster.Core.Alerting;
using Flownodes.Cluster.Core.Resources;
using Module = Autofac.Module;

namespace Flownodes.Cluster.Node.Modules;

public class ComponentsModule : Module
{
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
            .Where(x => typeof(IDataCollectorBehavior).IsAssignableFrom(x))
            .As<IDataCollectorBehavior>()
            .Keyed<IDataCollectorBehavior>(x => x.Name);

        builder.RegisterAssemblyTypes(assemblies)
            .Where(x => typeof(IDeviceBehavior).IsAssignableFrom(x))
            .As<IDeviceBehavior>()
            .Keyed<IDeviceBehavior>(x => x.Name);

        builder.RegisterAssemblyTypes(assemblies)
            .Where(x => typeof(IAlerterDriver).IsAssignableFrom(x))
            .As<IAlerterDriver>()
            .Keyed<IAlerterDriver>(x => x.Name);
    }
}