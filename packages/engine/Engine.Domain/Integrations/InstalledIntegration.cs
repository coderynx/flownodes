namespace Engine.Domain.Integrations;

/// <summary>
/// An Integration found in the deployment's integrations directory, loaded or not.
/// </summary>
public sealed class InstalledIntegration
{
    private InstalledIntegration()
    {
    }

    /// <summary>
    /// Gets the name of the folder the Integration was found in.
    /// </summary>
    public required string Folder { get; init; }
    /// <summary>
    /// Gets the id the Integration declares; absent when it failed before declaring one.
    /// </summary>
    public IntegrationId? Id { get; private init; }
    /// <summary>
    /// Gets the Integration's own version; absent when it never loaded.
    /// </summary>
    public string? Version { get; private init; }
    /// <summary>
    /// Gets the SDK version the Integration was built against; absent when none could be read.
    /// </summary>
    public string? SdkVersion { get; private init; }
    /// <summary>
    /// Gets the names of the Device Types the Integration defines; empty when it did not load.
    /// </summary>
    public IReadOnlyList<string> DeviceTypes { get; private init; } = [];
    /// <summary>
    /// Gets why the Integration did not load; absent when it loaded.
    /// </summary>
    public IntegrationLoadFailure? Failure { get; private init; }
    /// <summary>
    /// Gets the explanation that goes with <see cref="Failure"/>.
    /// </summary>
    public string? Message { get; private init; }
    /// <summary>
    /// Gets a value indicating whether the Integration loaded.
    /// </summary>
    public bool IsLoaded => Failure is null;

    /// <summary>
    /// Describes an Integration that loaded.
    /// </summary>
    /// <param name="folder">The folder it was found in.</param>
    /// <param name="id">The id it declares.</param>
    /// <param name="version">Its own version.</param>
    /// <param name="sdkVersion">The SDK version it was built against.</param>
    /// <param name="deviceTypes">The names of the Device Types it defines.</param>
    /// <returns>The loaded Integration.</returns>
    public static InstalledIntegration Loaded(
        string folder,
        IntegrationId id,
        string version,
        string sdkVersion,
        IReadOnlyList<string> deviceTypes) =>
        new()
        {
            Folder = folder,
            Id = id,
            Version = version,
            SdkVersion = sdkVersion,
            DeviceTypes = deviceTypes
        };

    /// <summary>
    /// Describes an Integration that did not load.
    /// </summary>
    /// <param name="folder">The folder it was found in.</param>
    /// <param name="failure">Why it did not load.</param>
    /// <param name="message">The explanation.</param>
    /// <param name="id">The id it declares, when it got that far.</param>
    /// <param name="sdkVersion">The SDK version it was built against, when it could be read.</param>
    /// <returns>The failed Integration.</returns>
    public static InstalledIntegration Failed(
        string folder,
        IntegrationLoadFailure failure,
        string message,
        IntegrationId? id = null,
        string? sdkVersion = null) =>
        new()
        {
            Folder = folder,
            Id = id,
            SdkVersion = sdkVersion,
            Failure = failure,
            Message = message
        };
}
