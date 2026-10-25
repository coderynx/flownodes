namespace Engine.Infrastructure.Connections;

[GenerateSerializer]
internal enum ConnectionPhase
{
    Unknown,
    Connecting,
    Connected,
    Degraded,
    Failed,
    IntegrationMissing,
    ConfigInvalid
}

[GenerateSerializer]
[Immutable]
internal sealed record ConnectionInfo([property: Id(0)] string? Integration, [property: Id(1)] ConnectionPhase Phase)
{
    public bool IsUp => Phase is ConnectionPhase.Connected or ConnectionPhase.Degraded;
}

[GenerateSerializer]
[Immutable]
internal sealed record DeviceRegistration(
    [property: Id(0)] string Name,
    [property: Id(1)] string Type,
    [property: Id(2)] string Address,
    [property: Id(3)] string Config);

[GenerateSerializer]
[Immutable]
internal sealed record CallResult(
    [property: Id(0)] bool Ok,
    [property: Id(1)] string Code,
    [property: Id(2)] string? Message,
    [property: Id(3)] string? Json)
{
    public static CallResult Success(string? json = null) => new(true, string.Empty, null, json);
    public static CallResult Failure(string code, string message) => new(false, code, message, null);
}
