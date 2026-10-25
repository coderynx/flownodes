using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Engine.Infrastructure.Integrations;

/// <summary>
/// Reads an assembly's metadata without loading it, so an Integration is judged before any of its code runs.
/// </summary>
internal static class EntryAssembly
{
    private const string SdkName = "Flownodes.Sdk";
    private const string AttributeName = "FlownodesIntegrationAttribute";

    /// <summary>
    /// Finds the assembly in a folder that carries the Integration attribute.
    /// </summary>
    /// <returns>The path and the SDK version it references, or <see langword="null"/> when none carries it.</returns>
    public static (string Path, Version SdkVersion)? Find(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*.dll").Order(StringComparer.Ordinal))
        {
            if (Inspect(file) is { } sdkVersion)
            {
                return (file, sdkVersion);
            }
        }

        return null;
    }

    private static Version? Inspect(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata)
            {
                return null;
            }

            var reader = pe.GetMetadataReader();
            var sdk = reader.AssemblyReferences.Select(handle => reader.GetAssemblyReference(handle))
                .Where(reference => reader.GetString(reference.Name) == SdkName)
                .Select(reference => reference.Version)
                .FirstOrDefault();

            return sdk is not null && CarriesAttribute(reader) ? sdk : null;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    private static bool CarriesAttribute(MetadataReader reader) =>
        reader.GetAssemblyDefinition()
            .GetCustomAttributes()
            .Select(handle => reader.GetCustomAttribute(handle))
            .Any(attribute => attribute.Constructor.Kind == HandleKind.MemberReference &&
                              reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent is
                                  { Kind: HandleKind.TypeReference } parent &&
                              reader.GetString(reader.GetTypeReference((TypeReferenceHandle)parent).Name) ==
                              AttributeName);
}
