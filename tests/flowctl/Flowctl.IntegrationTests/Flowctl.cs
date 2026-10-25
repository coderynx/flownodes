using System.Text.Json;

namespace Flowctl.IntegrationTests;

/// <summary>
/// Runs flowctl in process against a <see cref="StubServer"/>, with its own profile file and no waiting.
/// </summary>
public sealed class Flowctl : IDisposable
{
    public const string Server = "https://flownodes.test/";
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"flowctl-{Guid.NewGuid():N}");

    public Flowctl()
    {
        Directory.CreateDirectory(_directory);
    }

    public StubServer Stub { get; } = new();
    public string ProfilePath => Path.Combine(_directory, "flowctl.json");
    public Dictionary<string, string> Variables { get; } = [];
    public DateTimeOffset Now { get; set; } = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    public static Flowctl SignedIn()
    {
        var flowctl = new Flowctl();
        flowctl.WriteProfile(new { server = Server, accessToken = "token-1", expiresAt = "2030-01-01T00:00:00Z" });

        return flowctl;
    }

    public void WriteProfile(object profile) => File.WriteAllText(ProfilePath, JsonSerializer.Serialize(profile));
    public JsonElement ReadProfile() => JsonDocument.Parse(File.ReadAllText(ProfilePath)).RootElement.Clone();

    public string WriteFile(string name, string text)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, text);

        return path;
    }

    public async Task<Run> RunAsync(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var context = new CliContext(
            output,
            error,
            new StringReader(string.Empty),
            Stub,
            ProfilePath,
            name => Variables.GetValueOrDefault(name),
            (_, _) => Task.CompletedTask,
            new FixedTime(Now));

        var exitCode = await Cli.RunAsync(args, context);

        return new Run(exitCode, output.ToString(), error.ToString());
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
        Stub.Dispose();
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

public sealed record Run(int ExitCode, string Output, string Error);
