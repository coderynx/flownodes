namespace Engine.Infrastructure.Devices;

internal sealed class DeviceGrainOptions
{
    public const string Section = "Flownodes:Engine:DeviceGrain";
    public TimeSpan StatusInterval { get; set; } = TimeSpan.FromSeconds(1);
}
