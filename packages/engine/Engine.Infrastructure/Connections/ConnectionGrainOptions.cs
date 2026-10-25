namespace Engine.Infrastructure.Connections;

internal sealed class ConnectionGrainOptions
{
    public const string Section = "Flownodes:Engine:ConnectionGrain";
    public TimeSpan StatusInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan CallTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
