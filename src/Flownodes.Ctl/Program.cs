using System.Text.Json;
using Flownodes.Ctl.ApiSchemas;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Spectre.Console;

var builder = ConsoleApp.CreateBuilder(args);

builder.ConfigureServices(service =>
{
    service.AddRefitClient<IClusterApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5000"));
});

var app = builder.Build();

app.AddSubCommand("cluster", "get-info", async (IClusterApi clusterApi) =>
{
    AnsiConsole.MarkupLine("[bold green]Getting cluster info...[/] \n");
    var info = await clusterApi.GetClusterInfoAsync();
    var text = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
    AnsiConsole.WriteLine(text);
});

app.AddSubCommand("resource", "get-all", async (IClusterApi clusterApi) =>
{
    AnsiConsole.MarkupLine("[bold green]Getting resources...[/] \n");
    var response = await clusterApi.GetResourceSummariesAsync();
    if (response.IsSuccessStatusCode)
    {
        var text = JsonSerializer.Serialize(response.Content, new JsonSerializerOptions { WriteIndented = true });
        AnsiConsole.WriteLine(text);
        return;
    }

    AnsiConsole.WriteLine("[bold red]There was an error during the communication with the server[/]");
});

app.AddSubCommand("resource", "get", async (IClusterApi clusterApi, string id) =>
{
    AnsiConsole.MarkupLine($"[bold green]Getting resource {id} detail...[/] \n");
    var response = await clusterApi.GetResourceSummaryAsync(id);
    if (response.IsSuccessStatusCode)
    {
        var text = JsonSerializer.Serialize(response.Content, new JsonSerializerOptions { WriteIndented = true });
        AnsiConsole.WriteLine(text);
        return;
    }

    AnsiConsole.WriteLine("[bold red]There was an error during the communication with the server[/]");
});

await app.RunAsync();