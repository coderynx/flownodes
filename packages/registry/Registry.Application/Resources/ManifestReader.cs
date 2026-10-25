using System.Text.Json;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal sealed record ManifestEntry(
    int Document,
    string Kind,
    ResourceName Name,
    NamespaceName Namespace,
    long? Generation,
    JsonElement Spec,
    IResourceKind? Rules)
{
    public bool IsNamespace => Kind == Domain.Namespaces.Namespace.Kind;
    public string? NamespaceOrNull => IsNamespace ? null : Namespace.Value;

    public bool Is(Resource resource) =>
        resource.Namespace == Namespace && resource.Kind == Kind && resource.Name == Name;
}

internal sealed record ManifestReading(
    IReadOnlyList<ManifestEntry> Entries,
    IReadOnlyList<ManifestProblem> Problems,
    IReadOnlyList<string> Namespaces,
    bool TouchesNamespaces)
{
    public static ManifestReading Of(ManifestEntry entry) =>
        new([entry], [], entry.IsNamespace ? [] : [entry.Namespace.Value], entry.IsNamespace);
}

internal static class ManifestReader
{
    private static readonly string[] TopLevelFields = ["apiVersion", "kind", "metadata", "spec", "status"];
    private static readonly string[] MetadataFields = ["name", "namespace", "generation", "uid"];

    public static ManifestReading ForApply(
        IReadOnlyList<JsonElement> documents,
        string? requestedNamespace,
        KindCatalog catalog) =>
        Read(documents, requestedNamespace, catalog, true);

    public static ManifestReading ForDelete(
        IReadOnlyList<JsonElement> documents,
        string? requestedNamespace,
        KindCatalog catalog) =>
        Read(documents, requestedNamespace, catalog, false);

    private static ManifestReading Read(
        IReadOnlyList<JsonElement> documents,
        string? requestedNamespace,
        KindCatalog catalog,
        bool withSpec)
    {
        var entries = new List<ManifestEntry>();
        var problems = new List<ManifestProblem>();
        var namespaces = new SortedSet<string>(StringComparer.Ordinal);
        var touchesNamespaces = false;

        for (var index = 0; index < documents.Count; index++)
        {
            var document = new DocumentReader(index, documents[index], requestedNamespace, catalog, withSpec);
            var entry = document.ReadEntry();
            if (entry is not null && entries.Exists(other =>
                    other.Kind == entry.Kind && other.Name == entry.Name && other.Namespace == entry.Namespace))
            {
                document.Problem("metadata.name", $"{entry.Kind} '{entry.Name}' is listed more than once.");
                entry = null;
            }

            touchesNamespaces |= document.IsNamespace;
            if (document.Target is { } target)
            {
                namespaces.Add(target);
            }

            problems.AddRange(document.Problems);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return new ManifestReading(entries, problems, [.. namespaces], touchesNamespaces);
    }

    private sealed class DocumentReader(
        int index,
        JsonElement document,
        string? requestedNamespace,
        KindCatalog catalog,
        bool withSpec)
    {
        private string? _kind;
        private string? _name;
        public List<ManifestProblem> Problems { get; } = [];
        public bool IsNamespace => _kind == Namespace.Kind;
        public string? Target { get; private set; }

        public void Problem(string path, string message) =>
            Problems.Add(new ManifestProblem(index, _kind, _name, path, message));

        public ManifestEntry? ReadEntry()
        {
            if (document.ValueKind != JsonValueKind.Object)
            {
                Problem(string.Empty, "A Resource must be an object.");

                return null;
            }

            _kind = Text(document, "kind");
            if (document.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
            {
                _name = Text(metadata, "name");
            }

            foreach (var field in document.EnumerateObject().Where(field => !TopLevelFields.Contains(field.Name)))
            {
                Problem(field.Name, "Unknown field.");
            }

            ReadApiVersion();
            var rules = ReadKind();
            var (name, @namespace, generation) = ReadMetadata(metadata);
            var spec = withSpec ? ReadSpec() : Namespace.Spec;

            return Problems.Count == 0 && name is not null && @namespace is not null
                ? new ManifestEntry(index, _kind!, name, @namespace, generation, spec, rules)
                : null;
        }

        private static string? Text(JsonElement element, string field) =>
            element.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private void ReadApiVersion()
        {
            var present = document.TryGetProperty("apiVersion", out var value);
            var matches = value.ValueKind == JsonValueKind.String && value.GetString() == Resource.ApiVersion;
            var required = withSpec || present;
            if (required && !matches)
            {
                Problem("apiVersion", $"Must be '{Resource.ApiVersion}'.");
            }
        }

        private IResourceKind? ReadKind()
        {
            if (_kind is null)
            {
                Problem("kind", "Required.");

                return null;
            }

            var rules = catalog.Find(_kind);
            if (rules is null && !IsNamespace)
            {
                Problem("kind", $"There is no Kind called '{_kind}'.");
            }

            return rules;
        }

        private (ResourceName? Name, NamespaceName? Namespace, long? Generation) ReadMetadata(JsonElement metadata)
        {
            if (metadata.ValueKind != JsonValueKind.Object)
            {
                Target = IsNamespace ? null : requestedNamespace ?? NamespaceName.Default;
                Problem("metadata", "Required, with at least a name.");

                return (null, null, null);
            }

            foreach (var field in metadata.EnumerateObject().Where(field => !MetadataFields.Contains(field.Name)))
            {
                Problem($"metadata.{field.Name}", "Unknown field. A Resource has no labels or annotations.");
            }

            if (!ResourceName.TryCreate(_name, out var name))
            {
                Problem(
                    "metadata.name",
                    "Required, as a DNS label: lowercase letters, digits and hyphens, at most 63 characters, " +
                    "starting and ending with a letter or a digit.");
            }

            return (name, ReadNamespace(metadata, name), ReadGeneration(metadata));
        }

        private NamespaceName? ReadNamespace(JsonElement metadata, ResourceName? name)
        {
            var present = metadata.TryGetProperty("namespace", out var value);
            if (IsNamespace)
            {
                if (present)
                {
                    Problem("metadata.namespace", "A Namespace is installation-wide, so it has no Namespace.");
                }

                return name is null ? null : NamespaceName.Create(name.Value);
            }

            if (!present)
            {
                Target = requestedNamespace ?? NamespaceName.Default;

                return NamespaceName.TryCreate(Target, out var defaulted) ? defaulted : null;
            }

            var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            if (!NamespaceName.TryCreate(text, out var own))
            {
                Problem("metadata.namespace", "Must be the name of a Namespace.");

                return null;
            }

            Target = own.Value;
            if (requestedNamespace is not null && own.Value != requestedNamespace)
            {
                Problem("metadata.namespace", $"Is '{own}', but the request is for Namespace '{requestedNamespace}'.");

                return null;
            }

            return own;
        }

        private long? ReadGeneration(JsonElement metadata)
        {
            if (!metadata.TryGetProperty("generation", out var value) || !withSpec)
            {
                return null;
            }

            var generation = value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var whole) ? whole : 0;
            if (generation >= 1)
            {
                return generation;
            }

            Problem("metadata.generation", "Must be a whole number of at least 1.");

            return null;
        }

        private JsonElement ReadSpec()
        {
            var present = document.TryGetProperty("spec", out var spec);
            if (IsNamespace)
            {
                var isEmpty = spec.ValueKind == JsonValueKind.Object && !spec.EnumerateObject().Any();
                if (present && !isEmpty)
                {
                    Problem("spec", "A Namespace has nothing to configure, so its Spec must be empty.");
                }

                return Namespace.Spec;
            }

            if (!present || spec.ValueKind != JsonValueKind.Object)
            {
                Problem("spec", "Required, as an object.");
            }

            return spec;
        }
    }
}
