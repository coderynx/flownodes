using JasperFx;

namespace Flownodes;

internal sealed class Program
{
    private Program()
    {
    }

    private static async Task<int> Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Registry.Application.Bootstrap.DependencyInjection.AddApplication(builder);
        Registry.Infrastructure.Bootstrap.DependencyInjection.AddInfrastructure(builder);
        Registry.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);

        Identity.Application.Bootstrap.DependencyInjection.AddApplication(builder);
        Identity.Infrastructure.Bootstrap.DependencyInjection.AddInfrastructure(builder);
        Identity.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);

        Engine.Application.Bootstrap.DependencyInjection.AddApplication(builder);
        Engine.Infrastructure.Bootstrap.DependencyInjection.AddInfrastructure(builder);
        Engine.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);

        Web.Bootstrap.DependencyInjection.AddWeb(builder);

        var app = builder.Build();

        app.UseHttpsRedirection();

        Web.Bootstrap.HostConfiguration.UseWebSession(app);

        Registry.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);
        Registry.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);

        Identity.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);
        Identity.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);

        Engine.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);
        Engine.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);

        Web.Bootstrap.HostConfiguration.UseWeb(app);

        return await app.RunJasperFxCommands(args);
    }
}
