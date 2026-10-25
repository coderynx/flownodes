namespace Flowctl;

internal sealed record CliContext(
    TextWriter Output,
    TextWriter Error,
    TextReader Input,
    HttpMessageHandler Http,
    string ProfilePath,
    Func<string, string?> Variable,
    Func<TimeSpan, CancellationToken, Task> Delay,
    TimeProvider Time)
{
    public static CliContext FromProcess() =>
        new(
            Console.Out,
            Console.Error,
            Console.In,
            new SocketsHttpHandler(),
            Environment.GetEnvironmentVariable("FLOWCTL_CONFIG") ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config",
                "flownodes",
                "flowctl.json"),
            Environment.GetEnvironmentVariable,
            Task.Delay,
            TimeProvider.System);
}
