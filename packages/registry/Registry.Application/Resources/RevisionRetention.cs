namespace Registry.Application.Resources;

/// <summary>
/// How many Revisions the Registry keeps per Resource, set by <c>Flownodes:Registry:Revisions:Keep</c>.
/// </summary>
public sealed class RevisionRetention
{
    /// <summary>
    /// The setting section the retention binds from.
    /// </summary>
    public const string Section = "Flownodes:Registry:Revisions";

    /// <summary>
    /// Gets or sets how many of a Resource's newest Revisions are kept, tombstones included; older ones are dropped
    /// when a new one is recorded. The default is 50.
    /// </summary>
    public int Keep { get; set; } = 50;
}
