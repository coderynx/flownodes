using System.Text;
using System.Text.RegularExpressions;

string[] skipped = ["bin", "obj", "node_modules", "Generated"];
var root = Directory.GetCurrentDirectory();
var projects = new[] { "apps", "packages", "tests" }
    .SelectMany(directory => Directory.EnumerateFiles(
        Path.Combine(root, directory),
        "*.csproj",
        SearchOption.AllDirectories))
    .ToDictionary(
        project => Path.GetDirectoryName(project)!,
        project => Path.GetFileNameWithoutExtension(project));
var files = projects.Keys
    .SelectMany(project => Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories))
    .Where(file => !Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar).Any(skipped.Contains))
    .Distinct();

foreach (var file in files)
{
    var bytes = await File.ReadAllBytesAsync(file);
    var hasBom = bytes is [0xEF, 0xBB, 0xBF, ..];
    var text = Encoding.UTF8.GetString(bytes[(hasBom ? 3 : 0)..]);
    var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    var lines = text.Split(newline);

    var sorted = SortUsings(lines);
    var renamed = FixNamespace(lines, ExpectedNamespace(file));
    if (sorted || renamed)
    {
        await File.WriteAllTextAsync(file, string.Join(newline, lines), new UTF8Encoding(hasBom));
        Console.WriteLine(Path.GetRelativePath(root, file));
    }
}

string ExpectedNamespace(string file)
{
    var directory = Path.GetDirectoryName(file)!;
    var project = directory;

    while (!projects.ContainsKey(project))
    {
        project = Path.GetDirectoryName(project)!;
    }

    var suffix = Path.GetRelativePath(project, directory)
        .Replace(Path.DirectorySeparatorChar, '.');
    return suffix == "." ? projects[project] : $"{projects[project]}.{suffix}";
}

static bool SortUsings(string[] lines)
{
    var directive = new Regex(
        @"^using (static [\w.]+|[\w.]+|\w+ = [^;]+);$",
        RegexOptions.None,
        TimeSpan.FromSeconds(1));
    var indices = new List<int>();

    for (var i = 0; i < lines.Length; i++)
    {
        var line = lines[i].TrimEnd();
        if (directive.IsMatch(line))
        {
            indices.Add(i);
        }
        else if (line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
        {
            break;
        }
    }

    var usings = indices.Select(index => lines[index].TrimEnd()[6..^1]).ToList();
    var ordered = usings.OrderBy(name => name.Contains(" = ", StringComparison.Ordinal))
        .ThenBy(name => name is "System" || name.StartsWith("System.", StringComparison.Ordinal) ? 0 : 1)
        .ThenBy(name => name, StringComparer.Ordinal)
        .ToList();
    for (var i = 0; i < indices.Count; i++)
    {
        lines[indices[i]] = $"using {ordered[i]};";
    }

    return !usings.SequenceEqual(ordered);
}

static bool FixNamespace(string[] lines, string expected)
{
    var index = Array.FindIndex(lines, line => line.StartsWith("namespace ", StringComparison.Ordinal));
    if (index < 0)
    {
        return false;
    }

    var fixedLine = lines[index].TrimEnd().EndsWith(';') ? $"namespace {expected};" : $"namespace {expected}";
    var changed = lines[index].TrimEnd() != fixedLine;
    lines[index] = fixedLine;
    return changed;
}
