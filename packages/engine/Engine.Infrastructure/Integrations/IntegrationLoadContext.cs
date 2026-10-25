using System.Reflection;
using System.Runtime.Loader;

namespace Engine.Infrastructure.Integrations;

/// <summary>
/// One Integration's private assemblies. The framework, the SDK and the logging, dependency-injection and HTTP
/// client abstractions come from the host, so their types are the same on both sides of the boundary.
/// </summary>
internal sealed class IntegrationLoadContext(string entryPath) : AssemblyLoadContext(entryPath)
{
    private static readonly string[] Shared =
    [
        "Flownodes.Sdk",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.Http"
    ];

    private readonly AssemblyDependencyResolver _resolver = new(entryPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (Shared.Contains(assemblyName.Name))
        {
            return null;
        }

        return _resolver.ResolveAssemblyToPath(assemblyName) is { } path ? LoadFromAssemblyPath(path) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) =>
        _resolver.ResolveUnmanagedDllToPath(unmanagedDllName) is { } path
            ? LoadUnmanagedDllFromPath(path)
            : IntPtr.Zero;
}
