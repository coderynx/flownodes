namespace Engine.Infrastructure.Namespaces;

internal sealed class NamespaceGrainOptions
{
    public const string Section = "Flownodes:Engine:NamespaceGrain";
    public TimeSpan ReminderPeriod { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan PingPeriod { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan ResyncPeriod { get; set; } = TimeSpan.FromMinutes(15);
}
