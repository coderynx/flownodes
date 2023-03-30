using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Extendability;
using Flownodes.Sdk.Resourcing.Attributes;
using Flownodes.Sdk.Resourcing.Behaviours;
using Module = Autofac.Module;

namespace Flownodes.Worker.Extendability.Modules;

public class ExtensionsModule : Module
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
        const string extensionIdName = "extension_id";
        const string extensionDescriptionName = "extension_description";
        const string extensionAuthorName = "extension_author";
        
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        var scannerPattern = new[] { "Flownodes.Extensions.*.dll" };
        var extensionsPath = Path.Combine(Directory.GetCurrentDirectory(), "extensions");

        if (!Directory.Exists(extensionsPath)) Directory.CreateDirectory(extensionsPath);

        var assemblies = Directory
            .EnumerateFiles(extensionsPath, "*.dll", SearchOption.AllDirectories)
            .Where(filename => scannerPattern.Any(pattern => Regex.IsMatch(filename, pattern)))
            .Select(Assembly.LoadFrom)
            .ToArray();

        foreach (var assembly in assemblies)
        {
            var id = assembly.GetCustomAttribute<ExtensionIdAttribute>();
            var description = assembly.GetCustomAttribute<ExtensionDescriptionAttribute>();
            var author = assembly.GetCustomAttribute<ExtensionAuthorAttribute>();

            builder.RegisterAssemblyTypes(assembly)
                .Where(x => typeof(IBehaviour).IsAssignableFrom(x))
                .As<IBehaviour>()
                .Keyed<IBehaviour>(x => GetBehaviourName(x))
                .WithMetadata(extensionIdName, id)
                .WithMetadata(extensionDescriptionName, description)
                .WithMetadata(extensionAuthorName, author);

            builder.RegisterAssemblyTypes(assembly)
                .Where(x => typeof(IAlerterDriver).IsAssignableFrom(x))
                .As<IAlerterDriver>()
                .Keyed<IAlerterDriver>(x => GetAlerterDriverName(x))
                .WithMetadata(extensionIdName, id)
                .WithMetadata(extensionDescriptionName, description)
                .WithMetadata(extensionAuthorName, author);
        }
        

    }
}