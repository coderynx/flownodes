using System.Text.Json;
using System.Text.Json.Nodes;
using Flownodes.Ctl.ApiSchemas;
using Refit;
using Spectre.Console;

namespace Flownodes.Ctl.Commands;

[Command("resources")]
internal class ResourceCommands : ConsoleAppBase
{
    public ResourceCommands(IResourceApi resourceApi)
    {
        _resourceApi = resourceApi;
    }

    private readonly IResourceApi _resourceApi;
    
    
    [Command("get")]
    public async Task GetResources([Option("t")] string tenant, [Option("r")] string? resource = null)
    {
        ApiResponse<JsonNode> response;
    
        if (resource is null)
        {
            AnsiConsole.MarkupLine($"[bold green]Getting resources of {tenant}...[/] \n");
            response = await _resourceApi.GetResourcesAsync(tenant);
            if (response.IsSuccessStatusCode)
            {
                var text = JsonSerializer.Serialize(response.Content, new JsonSerializerOptions { WriteIndented = true });
                AnsiConsole.WriteLine(text);
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[bold green]Getting resource {resource} of tenant {tenant} detail...[/] \n");
            response = await _resourceApi.GetResourceAsync(tenant, resource);
            if (response.IsSuccessStatusCode)
            {
                var text = JsonSerializer.Serialize(response.Content, new JsonSerializerOptions { WriteIndented = true });
                AnsiConsole.WriteLine(text);
                return;
            }
        }
    
        AnsiConsole.WriteLine("[bold red]There was an error during the communication with the server[/]");
    }

    [Command("search")]
    public async Task SearchResources([Option("t")] string tenant, [Option("st")] string searchTerms)
    {
        AnsiConsole.MarkupLine($"[bold green]Searching resources of tenant {tenant} with terms {searchTerms}...[/] \n");
        var response = await _resourceApi.SearchResourcesAsync(tenant, searchTerms);
        
        if (response.IsSuccessStatusCode)
        {
            var text = JsonSerializer.Serialize(response.Content, new JsonSerializerOptions { WriteIndented = true });
            AnsiConsole.WriteLine(text);
            return;
        }
        
        AnsiConsole.WriteLine("[bold red]There was an error during the communication with the server[/]");
    }
}