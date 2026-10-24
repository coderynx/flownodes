using Projects;

namespace AppHost;

internal sealed class AppHost
{
    private AppHost()
    {
    }

    private static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var postgres = builder.AddPostgres("postgres").WithDataVolume();
        var engineDb = postgres.AddDatabase("engine");
        var registryDb = postgres.AddDatabase("registry");
        var identityDb = postgres.AddDatabase("identity");
        var installationKey = builder.AddParameter(
            "installation-key",
            new InstallationKeyDefault(),
            secret: true,
            persist: true);

        engineDb.WithOrleansProviderType("AdoNet");

        var orleans = builder.AddOrleans("engine-cluster")
            .WithClustering(engineDb)
            .WithGrainStorage("Default", engineDb);

        builder.AddProject<Flownodes>("flownodes", options => options.ExcludeLaunchProfile = true)
            .WithHttpEndpoint(name: "http")
            .WithHttpsEndpoint(name: "https")
            .WithReference(engineDb)
            .WaitFor(engineDb)
            .WithReference(orleans)
            .WithReference(registryDb)
            .WaitFor(registryDb)
            .WithReference(identityDb)
            .WaitFor(identityDb)
            .WithEnvironment("Flownodes__InstallationKey", installationKey)
            .WithUrlForEndpoint(
                "https",
                _ => new ResourceUrlAnnotation
                {
                    Url = "/scalar",
                    DisplayText = "Scalar"
                })
            .WithUrlForEndpoint(
                "https",
                _ => new ResourceUrlAnnotation
                {
                    Url = "/orleans",
                    DisplayText = "Orleans Dashboard"
                });

        builder.Build().Run();
    }
}
