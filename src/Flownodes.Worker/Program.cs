using Carter;
using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Services;
using Serilog;

namespace Flownodes.Worker;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddAuthorization();
        builder.Services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(GetTenantRequest).Assembly);
        });
        builder.Services.AddCarter();
        builder.Services.AddSingleton<IManagersService, ManagersService>();
        builder.Host.UseOrleans(ConfigureOrleans).UseSerilog(ConfigureLogging);

        var app = builder.Build();

        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapCarter();
        await app.RunAsync();
    }
}