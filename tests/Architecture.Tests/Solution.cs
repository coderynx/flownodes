using System.Reflection;
using System.Xml.Linq;

namespace Architecture.Tests;

internal static class Solution
{
    public static readonly string[] Layers = ["Contracts", "Domain", "Application", "Infrastructure", "Presentation"];
    public const string Core = "core";
    public const string Sdk = "sdk";
    public const string Web = "web";
    private static readonly string[] Ignored = ["bin", "obj", "node_modules"];

    public static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Flownodes.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Flownodes.slnx not found above the tests.");
    }

    public static string Relative(string path) => Path.GetRelativePath(Root(), path).Replace('\\', '/');

    public static IEnumerable<string> SourceFiles(params string[] roots) =>
        roots.Select(root => Path.Combine(Root(), root))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(file => !Relative(file).Split('/').Any(Ignored.Contains))
            .Where(file => !Relative(file).Contains("/Internal/Generated/", StringComparison.Ordinal));

    public static IEnumerable<string> HandWrittenSourceFiles() =>
        SourceFiles("apps", "packages", "integrations", "tests")
            .Where(file => !Relative(file).Contains("/Database/Migrations/"));

    public static IReadOnlyList<ModuleProject> ModuleProjects() =>
        Directory.EnumerateFiles(Path.Combine(Root(), "packages"), "*.csproj", SearchOption.AllDirectories)
            .Select(ModuleProject.From)
            .Where(project => project.Module is not (Sdk or Web))
            .ToList();

    public static IReadOnlyList<string> ProjectReferences(string project) =>
        XDocument.Load(project)
            .Descendants("ProjectReference")
            .Select(reference => (string?)reference.Attribute("Include") ?? string.Empty)
            .Select(include => Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(project)!, include.Replace('\\', '/'))))
            .ToList();

    public static IReadOnlyList<Assembly> Assemblies(string layer) =>
        ModuleProjects()
            .Where(project => project.Layer == layer)
            .Select(project => Assembly.Load(project.Name))
            .ToList();
}

internal sealed record ModuleProject(string Path, string Name, string Module, string Layer)
{
    public string Directory => System.IO.Path.GetDirectoryName(Path)!;

    public static ModuleProject From(string path)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        var module = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(path)));
        var layer = name[(name.LastIndexOf('.') + 1)..];

        return new ModuleProject(path, name, module!, layer);
    }
}
