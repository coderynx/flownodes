using System.Text.RegularExpressions;

namespace Architecture.Tests;

/// <summary>
///     A static class stays pure (ADR-0028): no static method takes a service, and the class reads no clock or
///     random source of its own. The check reads the production sources as text; a service is an interface
///     declared in the solution or one of the framework services the modules inject.
/// </summary>
internal static partial class StaticClasses
{
    private static readonly string[] ProductionRoots = ["packages", "apps"];

    private static readonly string[] FrameworkServices =
        ["TimeProvider", "UserManager", "DbContext", "IServiceProvider", "ISender", "IGrainFactory"];

    public static IReadOnlyList<string> Impure(string repoRoot)
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
                if (!relative.Split('/').Any(part => part is "bin" or "obj" or "Migrations" or "Generated"))
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
        var services = stripped.Values.SelectMany(text => Interface().Matches(text))
            .Select(match => match.Groups["name"].Value)
            .Concat(FrameworkServices)
            .ToHashSet();
        var impure = new List<string>();

        foreach (var (file, text) in stripped.Where(pair => !pair.Key.Contains("/Bootstrap/")))
        {
            foreach (Match declaration in StaticClass().Matches(text))
            {
                var name = declaration.Groups["name"].Value;
                var body = text[declaration.Index..DeadCode.DeclarationEnd(text, declaration.Index)];
                if (IsInjectedByAFramework(file, name, body))
                {
                    continue;
                }

                foreach (Match method in StaticMethod().Matches(body))
                {
                    var parameters = Parameters(body, method.Index + method.Length - 1);
                    impure.AddRange(
                        TypeName()
                            .Matches(parameters)
                            .Select(type => type.Value)
                            .Where(services.Contains)
                            .Distinct()
                            .Select(type => $"{file}: {name}.{method.Groups["name"].Value} takes {type}"));
                }

                impure.AddRange(
                    Ambient()
                        .Matches(body)
                        .Select(source => source.Value)
                        .Distinct()
                        .Select(source => $"{file}: {name} reads {source}"));
            }
        }

        return impure;
    }

    private static bool IsInjectedByAFramework(string file, string name, string body)
    {
        var isMessageHandler =
            file.Contains("/Messaging/") && name.EndsWith("MessageHandler", StringComparison.Ordinal);
        var mapsARoute = RouteMapping().IsMatch(body);

        return isMessageHandler || mapsARoute;
    }

    private static string Parameters(string text, int open)
    {
        var depth = 0;

        for (var index = open; index < text.Length; index++)
        {
            depth += text[index] switch { '(' => 1, ')' => -1, _ => 0 };
            if (depth == 0)
            {
                return text[(open + 1)..index];
            }
        }

        return text[(open + 1)..];
    }

    [GeneratedRegex(@"//[^\n]*")]
    private static partial Regex Comment();

    [GeneratedRegex(@"\binterface\s+(?<name>I[A-Z]\w*)")]
    private static partial Regex Interface();

    [GeneratedRegex(@"\bstatic\s+(?:partial\s+)?class\s+(?<name>\w+)")]
    private static partial Regex StaticClass();

    [GeneratedRegex(@"\bstatic\b[^;{}=]*?\b(?<name>\w+)\s*(?:<[^<>()]*>)?\s*\(")]
    private static partial Regex StaticMethod();

    [GeneratedRegex(@"\b[A-Z]\w*\b")]
    private static partial Regex TypeName();

    [GeneratedRegex(@"\.Map(?:Get|Post|Put|Delete|Patch)\s*\(")]
    private static partial Regex RouteMapping();

    [GeneratedRegex(
        @"\b(?:DateTime(?:Offset)?\.(?:Utc)?Now|TimeProvider\.System|RandomNumberGenerator|Random\.Shared|" +
        @"Guid\.(?:NewGuid|CreateVersion7))\b")]
    private static partial Regex Ambient();
}
