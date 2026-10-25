using System.CommandLine;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Flowctl;

internal sealed class Cli
{
    private const string Yaml = "application/yaml";
    private const string Json = "application/json";
    private const string Table = "table";
    private readonly CliContext _context;
    private readonly Printer _printer;

    private readonly Option<string?> _server = new("--server")
    {
        Description = "The Flownodes server, such as https://flownodes.local. Defaults to the one signed in to.",
        Recursive = true
    };

    private readonly Option<string?> _namespace = new("--namespace", "-n")
    {
        Description = "The Namespace to work in. Defaults to the Manifest's own, or default.",
        Recursive = true
    };

    private readonly Option<string?> _output = new("--output", "-o")
    {
        Description = "How to print what the server returns: table, yaml or json.",
        Recursive = true
    };

    private readonly Option<string[]> _files = new("--filename", "-f")
    {
        Description = "A Manifest file, YAML or JSON; '-' reads standard input. Repeat for several files.",
        AllowMultipleArgumentsPerToken = false
    };

    private readonly Option<bool> _dryRun = new("--dry-run")
    {
        Description = "Run every check and show what would change, without changing anything."
    };

    private Cli(CliContext context)
    {
        _context = context;
        _printer = new Printer(context);
        _output.AcceptOnlyFromAmong(Table, "yaml", "json");
    }

    public static Task<int> RunAsync(string[] args, CliContext context)
    {
        var root = new Cli(context).Root();

        return root.Parse(args)
            .InvokeAsync(
                new InvocationConfiguration
                {
                    Output = context.Output,
                    Error = context.Error
                });
    }

    private RootCommand Root()
    {
        var root = new RootCommand("flowctl applies, reads and deletes Flownodes Resources.");
        root.Options.Add(_server);
        root.Options.Add(_namespace);
        root.Options.Add(_output);
        root.Subcommands.Add(Login());
        root.Subcommands.Add(
            Apply("apply", "Apply a Manifest: create or update every Resource in it, or none.", false));
        root.Subcommands.Add(Apply("diff", "Show what applying a Manifest would change, without applying it.", true));
        root.Subcommands.Add(Get());
        root.Subcommands.Add(Delete());
        root.Subcommands.Add(Revisions());

        return root;
    }

    private Command Login()
    {
        var token = new Option<string?>("--token")
        {
            Description = "Sign in with a Personal Access Token instead of a browser."
        };
        var command = new Command("login", "Sign in to a Flownodes server.") { token };
        command.SetAction(async (parse, cancellationToken) =>
        {
            var serverText = parse.GetValue(_server) ?? Profile.Load(_context.ProfilePath).Server;
            if (serverText is null)
            {
                await _context.Error.WriteLineAsync("error: give the server to sign in to with --server.");

                return 1;
            }

            var server = Server(serverText);
            using var http = new HttpClient(_context.Http, false);
            var tokens = new Tokens(_context, http);
            var profile = parse.GetValue(token) is { } personalAccessToken
                ? await tokens.SignInWithPersonalAccessTokenAsync(server, personalAccessToken, cancellationToken)
                : await tokens.SignInWithDeviceAsync(server, cancellationToken);
            if (profile is null)
            {
                return 1;
            }

            profile.Save(_context.ProfilePath);
            await _context.Output.WriteLineAsync($"Signed in to {server}.");

            return 0;
        });

        return command;
    }

    private Command Apply(string name, string description, bool alwaysDryRun)
    {
        var command = new Command(name, description) { _files };
        if (!alwaysDryRun)
        {
            command.Options.Add(_dryRun);
        }

        command.SetAction(async (parse, cancellationToken) =>
        {
            var dryRun = alwaysDryRun || parse.GetValue(_dryRun);

            return await BatchAsync(parse, "apply", dryRun, alwaysDryRun || dryRun, cancellationToken);
        });

        return command;
    }

    private Command Get()
    {
        var kind = new Argument<string>("kind") { Description = "The Kind, such as widgets or namespace." };
        var name = new Argument<string?>("name")
        {
            Description = "The name of one Resource; every Resource of the Kind when omitted.",
            Arity = ArgumentArity.ZeroOrOne
        };
        var command = new Command("get", "Show Resources with their Spec, Status and Generation.") { kind, name };
        command.SetAction(async (parse, cancellationToken) =>
        {
            using var http = await ConnectAsync(parse, cancellationToken);
            if (http is null)
            {
                return 1;
            }

            var one = parse.GetValue(name);
            var @namespace = Namespace(parse);
            var output = parse.GetValue(_output) ?? (one is null ? Table : "yaml");
            var path = one is null
                ? Routes.Collection(parse.GetValue(kind)!, @namespace)
                : Routes.Item(parse.GetValue(kind)!, @namespace, one);

            return await ShowAsync(http, path, output, ResourceTableAsync, cancellationToken);
        });

        return command;
    }

    private Command Delete()
    {
        var kind = new Argument<string?>("kind") { Arity = ArgumentArity.ZeroOrOne, Description = "The Kind." };
        var name = new Argument<string?>("name") { Arity = ArgumentArity.ZeroOrOne, Description = "The name." };
        var command = new Command("delete", "Delete one Resource, or every Resource a Manifest lists, or none.")
        {
            kind,
            name,
            _files,
            _dryRun
        };
        command.SetAction(async (parse, cancellationToken) =>
        {
            var dryRun = parse.GetValue(_dryRun);
            if (parse.GetValue(_files) is { Length: > 0 })
            {
                return await BatchAsync(parse, "delete", dryRun, false, cancellationToken);
            }

            if (parse.GetValue(kind) is not { } kindText || parse.GetValue(name) is not { } nameText)
            {
                await _context.Error.WriteLineAsync("error: give a Kind and a name, or a Manifest with -f.");

                return 1;
            }

            using var http = await ConnectAsync(parse, cancellationToken);
            if (http is null)
            {
                return 1;
            }

            var path = Routes.Item(kindText, Namespace(parse), nameText) + (dryRun ? "?dryRun=true" : string.Empty);
            var response = await http.DeleteAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await _printer.ProblemAsync(response, cancellationToken);
            }

            var singular = Routes.Plural(kindText)[..^1];
            await _context.Output.WriteLineAsync(
                $"{singular}/{nameText} deleted{(dryRun ? " (dry run)" : string.Empty)}");

            return 0;
        });

        return command;
    }

    private Command Revisions()
    {
        var kind = new Argument<string>("kind") { Description = "The Kind." };
        var name = new Argument<string>("name") { Description = "The name of the Resource." };
        var command = new Command("revisions", "List the Revisions of a Resource, newest first.") { kind, name };
        command.SetAction(async (parse, cancellationToken) =>
        {
            using var http = await ConnectAsync(parse, cancellationToken);
            if (http is null)
            {
                return 1;
            }

            var path = Routes.Revisions(parse.GetValue(kind)!, Namespace(parse), parse.GetValue(name)!);

            return await ShowAsync(http, path, parse.GetValue(_output) ?? Table, RevisionTableAsync, cancellationToken);
        });

        return command;
    }

    private async Task<int> BatchAsync(
        ParseResult parse,
        string verb,
        bool dryRun,
        bool withChanges,
        CancellationToken cancellationToken)
    {
        if (parse.GetValue(_files) is not { Length: > 0 } files)
        {
            await _context.Error.WriteLineAsync("error: give a Manifest with -f.");

            return 1;
        }

        StringContent manifest;
        try
        {
            manifest = await Manifest.ReadAsync(files, _context.Input, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            await _context.Error.WriteLineAsync($"error: {exception.Message}");

            return 1;
        }

        using var http = await ConnectAsync(parse, cancellationToken);
        if (http is null)
        {
            return 1;
        }

        var response = await http.PostAsync(
            Routes.Batch(verb, parse.GetValue(_namespace), dryRun),
            manifest,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return await _printer.ProblemAsync(response, cancellationToken);
        }

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        await _printer.ChangesAsync(body.RootElement, withChanges);

        return 0;
    }

    private async Task<int> ShowAsync(
        HttpClient http,
        string path,
        string output,
        Func<JsonElement, Task> table,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(output == "yaml" ? Yaml : Json));
        var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return await _printer.ProblemAsync(response, cancellationToken);
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (output != Table)
        {
            await _context.Output.WriteLineAsync(text.TrimEnd());

            return 0;
        }

        using var body = JsonDocument.Parse(text);
        await table(body.RootElement);

        return 0;
    }

    private Task ResourceTableAsync(JsonElement body)
    {
        var items = body.TryGetProperty("items", out var list) ? list.EnumerateArray().ToList() : [body];
        var rows = items.Select(item =>
            {
                var metadata = item.GetProperty("metadata");
                var status = item.TryGetProperty("status", out var value) && value.ValueKind == JsonValueKind.Object
                    ? value
                    : (JsonElement?)null;

                return (IReadOnlyList<string>)
                [
                    Printer.Text(metadata, "name")!, metadata.GetProperty("generation").GetRawText(),
                    status?.TryGetProperty("observedGeneration", out var observed) == true
                        ? observed.GetRawText()
                        : "-",
                    status is { } reported ? Ready(reported) : "-"
                ];
            })
            .ToList();

        return _printer.TableAsync(["NAME", "GENERATION", "OBSERVED", "READY"], rows);
    }

    private Task RevisionTableAsync(JsonElement body)
    {
        var rows = body.GetProperty("items")
            .EnumerateArray()
            .Select(item => (IReadOnlyList<string>)
            [
                item.GetProperty("generation").GetRawText(),
                item.GetProperty("deleted").GetBoolean() ? "deleted" : "applied",
                Printer.Text(item.GetProperty("appliedBy"), "username") ??
                item.GetProperty("appliedBy").GetProperty("userId").GetString()!,
                item.GetProperty("appliedAt").GetDateTimeOffset().ToString("u")
            ])
            .ToList();

        return _printer.TableAsync(["GENERATION", "CHANGE", "BY", "AT"], rows);
    }

    private static string Ready(JsonElement status)
    {
        if (!status.TryGetProperty("conditions", out var conditions) || conditions.ValueKind != JsonValueKind.Array)
        {
            return "-";
        }

        var ready = conditions.EnumerateArray().FirstOrDefault(condition => Printer.Text(condition, "type") == "Ready");

        return ready.ValueKind == JsonValueKind.Object ? Printer.Text(ready, "status") ?? "-" : "-";
    }

    private async Task<HttpClient?> ConnectAsync(ParseResult parse, CancellationToken cancellationToken)
    {
        var profile = Profile.Load(_context.ProfilePath);
        var serverText = parse.GetValue(_server) ?? _context.Variable("FLOWCTL_SERVER") ?? profile.Server;
        if (serverText is null)
        {
            await _context.Error.WriteLineAsync("error: not signed in. Run: flowctl login --server <url>");

            return null;
        }

        var server = Server(serverText);
        var http = new HttpClient(_context.Http, false) { BaseAddress = server };
        var accessToken = await new Tokens(_context, http).AccessTokenAsync(server, profile, cancellationToken);
        if (accessToken is null)
        {
            http.Dispose();
            await _context.Error.WriteLineAsync($"error: not signed in to {server}. Run: flowctl login");

            return null;
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return http;
    }

    private string Namespace(ParseResult parse) => parse.GetValue(_namespace) ?? "default";
    private static Uri Server(string text) => new(text.EndsWith('/') ? text : $"{text}/", UriKind.Absolute);
}
