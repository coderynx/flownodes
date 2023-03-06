using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Tests.Configuration;
using Flownodes.Tests.Interfaces;
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

    [Fact]
    public async Task UpdateConfiguration_ShouldUpdateConfigurationAsync()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var configuration = _fixture.Create<Dictionary<string, object?>>();
        await grain.UpdateConfigurationAsync(configuration);

        var newConfiguration = await grain.GetConfiguration();
        newConfiguration.Should().BeEquivalentTo(configuration);
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldThrowWhenBehaviourIsNotRegistered()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var configuration = _fixture.Create<Dictionary<string, object?>>();
        configuration.Add("behaviourId", "unknown");

        var act = async () => { await grain.UpdateConfigurationAsync(configuration); };
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task UpdateMetadata_ShouldUpdateMetadata()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var metadata = _fixture.Create<Dictionary<string, string?>>();
        await grain.UpdateMetadataAsync(metadata);

        var newMetadata = await grain.GetMetadata();
        newMetadata.Proprties.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task GetState_ShouldGetState()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var state = await grain.GetState();
        state.Should().NotBeNull();
    }
}