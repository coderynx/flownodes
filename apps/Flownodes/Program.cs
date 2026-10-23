namespace Flownodes;

internal sealed class Program
{
    private Program()
    {
    }

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Registry.Application.Bootstrap.DependencyInjection.AddApplication(builder);
        Registry.Infrastructure.Bootstrap.DependencyInjection.AddInfrastructure(builder);
        Registry.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);

        Engine.Application.Bootstrap.DependencyInjection.AddApplication(builder);
        Engine.Infrastructure.Bootstrap.DependencyInjection.AddInfrastructure(builder);
        Engine.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);

        var app = builder.Build();

        app.UseHttpsRedirection();

        Registry.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);
        Registry.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);

        Engine.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);
        Engine.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);

        app.Run();
    }
}
