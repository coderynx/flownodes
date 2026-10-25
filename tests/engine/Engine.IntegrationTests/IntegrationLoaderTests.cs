using Engine.Domain.Integrations;
using Engine.Infrastructure.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class IntegrationLoaderTests
{
    [Fact]
    public void Load_ShouldDescribeTheIntegration_WhenAFolderHoldsTheSimulator()
    {
        // Arrange
        using var directory = new IntegrationDirectory();
        directory.AddSimulator("simulator");

        // Act
        var loads = Loader().Load(directory.Path);

        // Assert
        var installed = loads.ShouldHaveSingleItem().Installed;
        installed.Message.ShouldBeNull();
        installed.Id!.Value.ShouldBe("flownodes.simulator");
        installed.Version.ShouldBe("0.1.0");
        installed.SdkVersion.ShouldBe("0.1.0");
        installed.DeviceTypes.ShouldBe(["Thermometer", "Switch"]);
    }

    [Fact]
    public void Load_ShouldReportTheBrokenFolderAndLoadTheRest_WhenAFolderIsNotAnIntegration()
    {
        // Arrange
        using var directory = new IntegrationDirectory();
        directory.AddFolder("broken");
        directory.AddSimulator("simulator");

        // Act
        var loads = Loader().Load(directory.Path).Select(load => load.Installed).ToList();

        // Assert
        loads.Select(installed => installed.Folder).ShouldBe(["broken", "simulator"]);
        loads[0].Failure.ShouldBe(IntegrationLoadFailure.NotAnIntegration);
        loads[1].IsLoaded.ShouldBeTrue();
    }

    [Fact]
    public void Load_ShouldFailBothFolders_WhenTwoFoldersClaimOneId()
    {
        // Arrange
        using var directory = new IntegrationDirectory();
        directory.AddSimulator("first");
        directory.AddSimulator("second");

        // Act
        var loads = Loader().Load(directory.Path).Select(load => load.Installed).ToList();

        // Assert
        loads.ShouldAllBe(installed => installed.Failure == IntegrationLoadFailure.DuplicateId);
        loads.ShouldAllBe(installed => installed.Id!.Value == "flownodes.simulator");
    }

    [Fact]
    public void Load_ShouldRefuseTheIntegration_WhenItWasBuiltAgainstAnotherSdkMinor()
    {
        // Arrange
        using var directory = new IntegrationDirectory();
        directory.AddBuiltAgainstSdk("legacy", "0.9.0.0");

        // Act
        var installed = Loader().Load(directory.Path).ShouldHaveSingleItem().Installed;

        // Assert
        installed.Failure.ShouldBe(IntegrationLoadFailure.IncompatibleSdk);
        installed.SdkVersion.ShouldBe("0.9.0");
    }

    [Fact]
    public void Load_ShouldHandTheIntegrationOnlyTheSdkServices_WhenItBuildsItsContainer()
    {
        // Arrange
        using var directory = new IntegrationDirectory();
        directory.AddSimulator("simulator");
        var time = TimeProvider.System;

        // Act
        var services = Loader(time).Load(directory.Path).ShouldHaveSingleItem().Services!;

        // Assert
        services.GetService<TimeProvider>().ShouldBeSameAs(time);
        services.GetService<IHttpClientFactory>().ShouldNotBeNull();
        services.GetService<IConfiguration>().ShouldBeNull();
    }

    [Fact]
    public void Load_ShouldFindNothing_WhenTheDirectoryDoesNotExist()
    {
        // Act
        var loads = Loader().Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        // Assert
        loads.ShouldBeEmpty();
    }

    private static IntegrationLoader Loader(TimeProvider? time = null) =>
        new(NullLoggerFactory.Instance, time ?? TimeProvider.System, new StubHttpClients());

    private sealed class StubHttpClients : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
