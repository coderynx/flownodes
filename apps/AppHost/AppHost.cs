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
        var webDb = postgres.AddDatabase("web");
        var installationKey = builder.AddParameter(
            "installation-key",
            new InstallationKeyDefault(),
            secret: true,
            persist: true);

        engineDb.WithOrleansProviderType("AdoNet");

        var orleans = builder.AddOrleans("engine-cluster")
            .WithClustering(engineDb)
            .WithGrainStorage("Default", engineDb)
            .WithReminders(engineDb);

        var webClient = builder.AddViteApp(
            "web-client",
            Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "packages", "web", "Web.Client")));

        var flownodes = builder.AddProject<Flownodes>("flownodes", launchProfileName: "https")
            .WithReference(engineDb)
            .WaitFor(engineDb)
            .WithReference(orleans)
            .WithReference(registryDb)
            .WaitFor(registryDb)
            .WithReference(identityDb)
            .WaitFor(identityDb)
            .WithReference(webDb)
            .WaitFor(webDb)
            .WaitFor(webClient)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
            .WithEnvironment("Flownodes__InstallationKey", installationKey)
            .WithEnvironment("Flownodes__Web__DevServer", webClient.GetEndpoint("http"))
            .WithEnvironment(
                "Flownodes__Integrations__Directory",
                Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "artifacts", "integrations")))
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

        flownodes.WithEnvironment("Flownodes__Identity__Authority", flownodes.GetEndpoint("https"));

        builder.Build().Run();
    }
}
