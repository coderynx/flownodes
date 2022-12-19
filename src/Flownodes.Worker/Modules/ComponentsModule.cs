using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Flownodes.Core.Interfaces;
using Throw;
using Module = Autofac.Module;

namespace Flownodes.Worker.Modules;

public class ComponentsModule : Module
{
    private static string GetAttributeName(MemberInfo type)
    {
        var attribute = type.GetCustomAttribute(typeof(DeviceIdAttribute)) as DeviceIdAttribute;
        attribute.ThrowIfNull();
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
            .Keyed<IBehaviour>(x => GetAttributeName(x));
    }
}