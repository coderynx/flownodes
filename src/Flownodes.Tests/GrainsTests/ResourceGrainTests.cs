using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Models;
using Flownodes.Tests.Configuration;
using Flownodes.Worker.Interfaces;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class ResourceGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public ResourceGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    private ResourceConfigurationStore ProvideResourceConfiguration()
    {
        var configuration = _fixture.Create<ResourceConfigurationStore>();
        configuration.BehaviourId = "TestDeviceBehavior";
        return configuration;
    }

    [Fact]
    public async Task ShouldUpdateConfigurationAsync()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var configuration = ProvideResourceConfiguration();
        await grain.UpdateConfigurationAsync(configuration);

        var newConfiguration = await grain.GetConfiguration();
        newConfiguration.Should().BeEquivalentTo(configuration);
    }

    [Fact]
    public async Task ShouldNotUpdateConfiguration_WhenBehaviourIsInvalid()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var configuration = _fixture.Create<ResourceConfigurationStore>();
        configuration.BehaviourId = "InvalidBehaviour";

        var act = async () => { await grain.UpdateConfigurationAsync(configuration); };
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ShouldUpdateMetadata()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var metadata = _fixture.Create<Dictionary<string, string?>>();
        await grain.UpdateMetadataAsync(metadata);

        var newMetadata = await grain.GetMetadata();
        newMetadata.Properties.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task ShouldGetState()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var state = await grain.GetState();
        state.Should().NotBeNull();
    }
}