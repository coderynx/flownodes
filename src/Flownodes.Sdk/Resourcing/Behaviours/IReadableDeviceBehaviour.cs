namespace Flownodes.Sdk.Resourcing.Behaviours;

public sealed record UpdateResourceBag
{
    public UpdateResourceBag(Dictionary<string, object?> metadata, Dictionary<string, object?> configuration,
        Dictionary<string, object?> state)
    {
        Metadata = metadata;
        Configuration = configuration;
        State = state;
    }

    public UpdateResourceBag()
    {
    }

    public Dictionary<string, object?> Metadata { get; set; } = new();
    public Dictionary<string, object?> Configuration { get; set; } = new();
    public Dictionary<string, object?> State { get; set; } = new();
}

/// <summary>
///     The interface for implementing custom device behaviours with readable state.
/// </summary>
public interface IReadableDeviceBehaviour : IDeviceBehaviour
{
    /// <summary>
    ///     Code to execute when state state pull from device is requested.
    /// </summary>
    /// <returns></returns>
    Task<UpdateResourceBag> OnPullStateAsync();
}