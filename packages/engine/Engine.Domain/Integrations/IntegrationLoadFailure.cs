namespace Engine.Domain.Integrations;

/// <summary>
/// Why an Integration in the deployment did not load.
/// </summary>
public enum IntegrationLoadFailure
{
    /// <summary>
    /// The folder holds no assembly marked as an Integration's entry assembly.
    /// </summary>
    NotAnIntegration,

    /// <summary>
    /// The Integration was built against an SDK version this Flownodes does not support.
    /// </summary>
    IncompatibleSdk,

    /// <summary>
    /// The Integration declares an id that is not vendor-qualified.
    /// </summary>
    InvalidId,

    /// <summary>
    /// Another folder declares the same id, so neither loads.
    /// </summary>
    DuplicateId,

    /// <summary>
    /// The Integration threw or broke the SDK contract while loading.
    /// </summary>
    LoadFailed
}
