using System;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk;
using Flownodes.Tests.Fixtures;
using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Services;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class PluginsContainerTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public PluginsContainerTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId ProvideFakeFlownodesId()
    {
        return new FlownodesId(FlownodesObject.Other, _fixture.Create<string>(), _fixture.Create<string>());
    }

    [Fact]
    public async Task GetService_ShouldNotResolveMainContainerServices()
    {
        // Arrange.
        var testGrain = _cluster.GrainFactory.GetGrain<ITestResourceGrain>(ProvideFakeFlownodesId());

        // Act & Assert.
        var act = async () => { await testGrain.GetService<IEnvironmentService>(); };
        await act.Should().ThrowAsync<Exception>();
    }
}