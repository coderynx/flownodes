using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Engine.IntegrationTests;

/// <summary>
/// A scratch integrations directory that tests fill with copies of the simulator and with hand-made folders.
/// </summary>
public sealed class IntegrationDirectory : IDisposable
{
    public IntegrationDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"integrations-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void AddSimulator(string folder)
    {
        var source = System.IO.Path.Combine(RepositoryRoot(), "artifacts", "integrations", "flownodes.simulator");
        var target = Directory.CreateDirectory(System.IO.Path.Combine(Path, folder)).FullName;

        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)));
        }
    }

    public void AddFolder(string folder, string fileName = "readme.txt")
    {
        var target = Directory.CreateDirectory(System.IO.Path.Combine(Path, folder)).FullName;
        File.WriteAllText(System.IO.Path.Combine(target, fileName), "not an integration");
    }

    /// <summary>
    /// Adds an Integration compiled against a stand-in SDK of the given version, which no host supports.
    /// </summary>
    public void AddBuiltAgainstSdk(string folder, string sdkVersion)
    {
        var sdk = Compile(
            "Flownodes.Sdk",
            sdkVersion,
            "namespace Flownodes.Sdk { public sealed class FlownodesIntegrationAttribute(System.Type t) : System.Attribute { } }");
        var entry = Compile(
            "Acme.Legacy",
            "1.0.0.0",
            "[assembly: Flownodes.Sdk.FlownodesIntegration(typeof(Acme.Legacy.Entry))] namespace Acme.Legacy { public sealed class Entry { } }",
            sdk);
        var target = Directory.CreateDirectory(System.IO.Path.Combine(Path, folder)).FullName;
        File.WriteAllBytes(System.IO.Path.Combine(target, "Flownodes.Sdk.dll"), sdk);
        File.WriteAllBytes(System.IO.Path.Combine(target, "Acme.Legacy.dll"), entry);
    }

    public void Dispose() => Directory.Delete(Path, true);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "Flownodes.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Flownodes.slnx not found above the tests.");
    }

    private static byte[] Compile(string name, string version, string source, byte[]? sdk = null)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(System.IO.Path.PathSeparator)
            .Where(path => System.IO.Path.GetFileNameWithoutExtension(path) != "Flownodes.Sdk")
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
        if (sdk is not null)
        {
            references.Add(MetadataReference.CreateFromImage(sdk));
        }

        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText($"[assembly: System.Reflection.AssemblyVersion(\"{version}\")] {source}")],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        }

        return stream.ToArray();
    }
}
