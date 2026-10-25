namespace Flowctl;

internal static class Routes
{
    private const string Registry = "api/v1/registry";
    private const string Namespaces = "namespaces";
    public static string Integrations => "api/v1/engine/integrations";
    public static bool IsIntegrations(string kind) => kind.ToLowerInvariant() is "integration" or "integrations";

    public static string Collection(string kind, string @namespace)
    {
        var route = kind.ToLowerInvariant();

        return route is Namespaces or "namespace"
            ? $"{Registry}/{Namespaces}"
            : $"{Registry}/{Namespaces}/{Escape(@namespace)}/{Escape(route)}";
    }

    public static string Item(string kind, string @namespace, string name) =>
        $"{Collection(kind, @namespace)}/{Escape(name)}";

    public static string Revisions(string kind, string @namespace, string name) =>
        $"{Item(kind, @namespace, name)}/revisions";

    public static string Batch(string verb, string? @namespace, bool dryRun)
    {
        var query = new List<string>();
        if (@namespace is not null)
        {
            query.Add($"namespace={Escape(@namespace)}");
        }

        if (dryRun)
        {
            query.Add("dryRun=true");
        }

        return query.Count == 0 ? $"{Registry}/{verb}" : $"{Registry}/{verb}?{string.Join('&', query)}";
    }

    private static string Escape(string segment) => Uri.EscapeDataString(segment);
}
