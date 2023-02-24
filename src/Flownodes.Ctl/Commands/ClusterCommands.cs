using System.Text.Json;
using Flownodes.Ctl.ApiSchemas;
using Spectre.Console;

namespace Flownodes.Ctl.Commands;

[Command("cluster")]
public class ClusterCommands : ConsoleAppBase
{
    public ClusterCommands(IClusterApi clusterApi)
    {
        _clusterApi = clusterApi;
    }

    private readonly IClusterApi _clusterApi;

    [Command("get")]
    public async Task GetClusterInfoAsync()
    {
        AnsiConsole.MarkupLine($"[bold green]Getting cluster info...[/] \n");
        var response = await _clusterApi.GetInfoAsync();
        if (response.IsSuccessStatusCode)
        {
            var text = JsonSerializer.Serialize(response.Content, new JsonSerializerOptions { WriteIndented = true });
            AnsiConsole.WriteLine(text);
            return;
        }
        
        AnsiConsole.WriteLine("[bold red]There was an error during the communication with the server[/]");
    }
}