using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Serilog;
using Module = Autofac.Module;

namespace Flownodes.Worker.Extendability.Modules;

public class ComponentsContainerModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (appPath is null) throw new NullReferenceException("Application path should not be null");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(appPath)
            .AddJsonFile("pluginsConfiguration.json", true)
            .Build();

        var pluginServices = new ServiceCollection();
        pluginServices.AddLogging(loggingBuilder =>
        {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            loggingBuilder.AddSerilog(logger);
        });
        pluginServices.AddHttpClient();

        builder.Populate(pluginServices);
        builder.RegisterInstance<IConfiguration>(configuration.GetSection("Plugins"));
    }
}