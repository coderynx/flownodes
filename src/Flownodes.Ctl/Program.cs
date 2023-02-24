using System.Text.Json;
using System.Text.Json.Nodes;
using Flownodes.Ctl.ApiSchemas;
using Flownodes.Ctl.Commands;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Spectre.Console;

var builder = ConsoleApp.CreateBuilder(args);

builder.ConfigureServices(service =>
{
    service.AddRefitClient<IClusterApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5000"));
    
    service.AddRefitClient<IResourceApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5000"));
});

var app = builder.Build();
app.AddSubCommands<ResourceCommands>();

await app.RunAsync();