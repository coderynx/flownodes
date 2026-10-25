namespace Registry.Application.Resources;

internal sealed class RevisionRetention
{
    public const string Section = "Flownodes:Registry:Revisions";
    public int Keep { get; set; } = 50;
}
