namespace Engine.Domain.Integrations;

/// <summary>
/// Decides which SDK versions this Flownodes can run Integrations built against.
/// </summary>
public static class SdkCompatibility
{
    /// <summary>
    /// Decides whether an Integration built against one SDK version may run on a host that ships another: the major
    /// versions must match and, while the SDK is <c>0.x</c>, the minor versions must match exactly too.
    /// </summary>
    /// <param name="host">The SDK version the host ships.</param>
    /// <param name="builtAgainst">The SDK version the Integration was built against.</param>
    /// <returns><see langword="true"/> when the host supports the Integration.</returns>
    public static bool Supports(Version host, Version builtAgainst) =>
        host.Major == builtAgainst.Major && (host.Major != 0 || host.Minor == builtAgainst.Minor);
}
