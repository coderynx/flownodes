using System.Reflection;
using Engine.Domain.Integrations;
using Flownodes.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Engine.Infrastructure.Integrations;

/// <summary>
/// Loads every Integration folder of a directory. One that cannot load is reported and never stops the others.
/// </summary>
internal sealed class IntegrationLoader(ILoggerFactory loggers, TimeProvider time, IHttpClientFactory httpClients)
{
    private static readonly Version HostSdk = typeof(IIntegration).Assembly.GetName().Version!;

    public IReadOnlyList<IntegrationLoad> Load(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var candidates = Directory.EnumerateDirectories(directory)
            .Order(StringComparer.Ordinal)
            .Select(Prepare)
            .ToList();

        var duplicates = candidates.Where(candidate => candidate.Instance is not null)
            .GroupBy(candidate => candidate.Id!.Value)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .ToHashSet();

        return
        [
            .. candidates.Select(candidate => duplicates.Contains(candidate) ? Duplicate(candidate) : Start(candidate))
        ];
    }

    private static Candidate Prepare(string folderPath)
    {
        var folder = Path.GetFileName(folderPath);
        try
        {
            if (EntryAssembly.Find(folderPath) is not var (path, sdkVersion))
            {
                return Candidate.Fail(
                    folder,
                    IntegrationLoadFailure.NotAnIntegration,
                    "No assembly in the folder carries [assembly: FlownodesIntegration].");
            }

            if (!SdkCompatibility.Supports(HostSdk, sdkVersion))
            {
                return Candidate.Fail(
                    folder,
                    IntegrationLoadFailure.IncompatibleSdk,
                    $"Built against SDK {Format(sdkVersion)}; this Flownodes supports SDK {Format(HostSdk)}.",
                    sdkVersion: Format(sdkVersion));
            }

            var assembly = new IntegrationLoadContext(path).LoadFromAssemblyPath(path);
            var entryType = assembly.GetCustomAttribute<FlownodesIntegrationAttribute>()!.EntryType;
            if (Activator.CreateInstance(entryType) is not IIntegration instance)
            {
                return Candidate.Fail(
                    folder,
                    IntegrationLoadFailure.LoadFailed,
                    $"{entryType.FullName} does not implement IIntegration.",
                    sdkVersion: Format(sdkVersion));
            }

            if (!IntegrationId.TryCreate(instance.Id, out var id))
            {
                return Candidate.Fail(
                    folder,
                    IntegrationLoadFailure.InvalidId,
                    $"'{instance.Id}' is not a vendor-qualified id such as 'flownodes.mqtt'.",
                    sdkVersion: Format(sdkVersion));
            }

            return new Candidate(folder, instance, id, Version(assembly), Format(sdkVersion), null);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Candidate.Fail(folder, IntegrationLoadFailure.LoadFailed, exception.Message);
        }
    }

    private IntegrationLoad Start(Candidate candidate)
    {
        if (candidate.Instance is not { } instance)
        {
            return new IntegrationLoad(candidate.Failed!, null);
        }

        try
        {
            var services = new ServiceCollection();
            services.AddSingleton(loggers);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton(time);
            services.AddSingleton(httpClients);
            instance.ConfigureServices(services);
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
            var deviceTypes = instance.DeviceTypes.Select(deviceType => deviceType.Name).ToList();

            return new IntegrationLoad(
                InstalledIntegration.Loaded(
                    candidate.Folder,
                    candidate.Id!,
                    candidate.EntryVersion!,
                    candidate.SdkVersion!,
                    deviceTypes),
                provider);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return new IntegrationLoad(
                InstalledIntegration.Failed(
                    candidate.Folder,
                    IntegrationLoadFailure.LoadFailed,
                    exception.Message,
                    candidate.Id,
                    candidate.SdkVersion),
                null);
        }
    }

    private static IntegrationLoad Duplicate(Candidate candidate) =>
        new(
            InstalledIntegration.Failed(
                candidate.Folder,
                IntegrationLoadFailure.DuplicateId,
                $"Another folder also declares '{candidate.Id}'.",
                candidate.Id,
                candidate.SdkVersion),
            null);

    private static string Version(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return informational?.Split('+')[0] ?? Format(assembly.GetName().Version ?? new Version(0, 0));
    }

    private static string Format(Version version) => version.Build < 0 ? version.ToString() : version.ToString(3);

    private sealed record Candidate(
        string Folder,
        IIntegration? Instance,
        IntegrationId? Id,
        string? EntryVersion,
        string? SdkVersion,
        InstalledIntegration? Failed)
    {
        public static Candidate Fail(
            string folder,
            IntegrationLoadFailure failure,
            string message,
            string? sdkVersion = null) =>
            new(
                folder,
                null,
                null,
                null,
                sdkVersion,
                InstalledIntegration.Failed(folder, failure, message, null, sdkVersion));
    }
}

/// <summary>
/// What loading one Integration folder produced: its description and, when it loaded, its private container.
/// </summary>
internal sealed record IntegrationLoad(InstalledIntegration Installed, IServiceProvider? Services);
