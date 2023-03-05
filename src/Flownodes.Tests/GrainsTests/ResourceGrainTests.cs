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
    public async Task ShouldUpdateConfigurationAsync()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var configuration = _fixture.Create<Dictionary<string, object?>>();
        await grain.UpdateConfigurationAsync(configuration);

        var newConfiguration = await grain.GetConfiguration();
        newConfiguration.Properties.Should().BeEquivalentTo(configuration);
    }

    [Fact]
    public async Task ShouldUpdateMetadata()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var metadata = _fixture.Create<Dictionary<string, string?>>();
        await grain.UpdateMetadataAsync(metadata);

        var newMetadata = await grain.GetMetadata();
        newMetadata.Proprties.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task ShouldGetState()
    {
        var grain = _cluster.GrainFactory.GetGrain<IDummyResourceGrain>(_fixture.Create<string>());

        var state = await grain.GetState();
        state.Should().NotBeNull();
    }
}