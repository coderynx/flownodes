namespace Engine.Infrastructure.Namespaces;

internal sealed class NamespaceListOptions
{
    public const string Section = "Flownodes:Engine:NamespaceList";
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
}
