var builder = WebApplication.CreateBuilder(args);

Registry.Application.Bootstrap.DependencyInjection.AddApplication(builder);
Registry.Infrastructure.Bootstrap.DependencyInjection.AddInfrastructure(builder);
Registry.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);

Engine.Application.Bootstrap.DependencyInjection.AddApplication(builder);
Engine.Infrastructure.Bootstrap.DependencyInjection.AddInfrastructure(builder);
Engine.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);

var app = builder.Build();

app.UseHttpsRedirection();

Registry.Application.Bootstrap.HostConfiguration.UseApplication(app);
Registry.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);
Registry.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);

Engine.Application.Bootstrap.HostConfiguration.UseApplication(app);
Engine.Infrastructure.Bootstrap.HostConfiguration.UseInfrastructure(app);
Engine.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);

app.Run();
