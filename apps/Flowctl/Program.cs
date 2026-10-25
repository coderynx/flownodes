namespace Flowctl;

internal sealed class Program
{
    private Program()
    {
    }

    private static Task<int> Main(string[] args) => Cli.RunAsync(args, CliContext.FromProcess());
}
