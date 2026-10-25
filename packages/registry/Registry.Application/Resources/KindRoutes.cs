using Registry.Domain.Namespaces;

namespace Registry.Application.Resources;

internal static class KindRoutes
{
    private const string Singular = "namespace";
    public static bool IsNamespace(string kinds) => kinds is Namespace.Plural or Singular;
}
