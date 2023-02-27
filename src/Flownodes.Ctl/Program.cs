using Flownodes.Ctl.ApiSchemas;
using Microsoft.Extensions.DependencyInjection;
using Refit;

var builder = ConsoleApp.CreateBuilder(args);

builder.ConfigureServices(service =>
{
    service.AddRefitClient<IClusterApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5000"));

    service.AddRefitClient<IResourceApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5000"));
});

var app = builder.Build();
app.AddAllCommandType();

await app.RunAsync();