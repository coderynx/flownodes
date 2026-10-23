using System.Text.RegularExpressions;

namespace Architecture.Tests;

/// <summary>
///     A type nothing references is dead code (ADR-0024). Roslyn sees one project at a time, so the
///     cross-project check reads the production sources of the whole solution as text: a type is live
///     when its name appears in another file, or in the same file outside its own declaration.
/// </summary>
internal static partial class DeadCode
{
    private static readonly string[] ProductionRoots = ["packages", "apps"];

    public static IReadOnlyList<string> Unreferenced(string repoRoot)
    {
        var sources = new Dictionary<string, string>();

        foreach (var root in ProductionRoots)
        {
            foreach (var file in Directory.EnumerateFiles(
                         Path.Combine(repoRoot, root),
                         "*.cs",
                         SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
                if (!relative.Split('/').Any(part => part is "bin" or "obj" or "Migrations"))
                {
                    sources[relative] = File.ReadAllText(file);
                }
            }
        }

        return Find(sources);
    }

    public static IReadOnlyList<string> Find(IReadOnlyDictionary<string, string> sources)
    {
        var stripped = sources.ToDictionary(pair => pair.Key, pair => Comment().Replace(pair.Value, string.Empty));
        var dead = new List<string>();

        foreach (var (file, text) in stripped)
        {
            foreach (var declaration in Declaration().Matches(text).Where(match => !IsInvokedByAFramework(text, match)))
            {
                var name = declaration.Groups["name"].Value;
                var reference = new Regex($@"\b{Regex.Escape(name)}\b");
                var end = DeclarationEnd(text, declaration.Index);
                var usedInAnotherFile = stripped.Any(other => other.Key != file && reference.IsMatch(other.Value));
                var usedOutsideItself = reference.IsMatch(text[..declaration.Index]) || reference.IsMatch(text[end..]);
                if (!usedInAnotherFile && !usedOutsideItself)
                {
                    dead.Add($"{file}: {name} is never referenced");
                }
            }
        }

        return dead;
    }

    private static bool IsInvokedByAFramework(string text, Match declaration)
    {
        var body = text[declaration.Index..DeclarationEnd(text, declaration.Index)];
        var isEntityConfiguration = body.Contains("IEntityTypeConfiguration<", StringComparison.Ordinal);
        var isRequestHandler = RequestHandler().IsMatch(body);
        var isEndpoint = body.Contains(": IEndpoint", StringComparison.Ordinal);
        var isEntryPoint = EntryPoint().IsMatch(body);
        var holdsExtensionMethods = Regex.IsMatch(body, @"\(\s*this\s+");

        return isEntityConfiguration || isRequestHandler || isEndpoint || isEntryPoint || holdsExtensionMethods;
    }

    private static int DeclarationEnd(string text, int start)
    {
        var open = text.IndexOfAny(['{', ';'], start);
        if (open < 0 || text[open] == ';')
        {
            return open < 0 ? text.Length : open + 1;
        }

        var depth = 0;

        for (var index = open; index < text.Length; index++)
        {
            depth += text[index] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0)
            {
                return index + 1;
            }
        }

        return text.Length;
    }

    [GeneratedRegex(@"\b(IRequestHandler|ICommandHandler|IQueryHandler)<")]
    private static partial Regex RequestHandler();

    [GeneratedRegex(@"\bstatic\s+(async\s+)?(void|int|Task|Task<int>)\s+Main\s*\(")]
    private static partial Regex EntryPoint();

    [GeneratedRegex(@"//[^\n]*")]
    private static partial Regex Comment();

    [GeneratedRegex(
        @"^[ \t]*(?:(?:public|internal|private|protected|sealed|static|abstract|partial|readonly|file)\s+)*" +
        @"(?:class|record\s+struct|record\s+class|record|struct|interface|enum)\s+(?<name>\w+)",
        RegexOptions.Multiline)]
    private static partial Regex Declaration();
}
