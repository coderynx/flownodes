using Registry.Domain.Namespaces;

namespace Registry.Presentation.Namespaces.Contracts;

internal sealed record NamespaceDto(string Name);

internal static class NamespaceMappings
{
    public static NamespaceDto ToDto(this Namespace @namespace) => new(@namespace.Name.Value);
}
