namespace Engine.Infrastructure.Flows;

internal sealed class FlowGrainOptions
{
    public const string Section = "Flownodes:Engine:FlowGrain";
    public TimeSpan StatusInterval { get; set; } = TimeSpan.FromSeconds(1);
    public string TimeZone { get; set; } = "UTC";
}
