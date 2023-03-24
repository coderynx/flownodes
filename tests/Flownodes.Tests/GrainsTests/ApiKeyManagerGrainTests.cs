using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Users;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class ApiKeyManagerGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public ApiKeyManagerGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId NewFlownodesId => new(FlownodesEntity.ApiKeyManager, _fixture.Create<string>());

    private IApiKeyManagerGrain NewApiKeyManagerGrain =>
        _cluster.GrainFactory.GetGrain<IApiKeyManagerGrain>(NewFlownodesId);

    [Fact]
    public async Task GenerateApiKey_ShouldGenerateApiKey()
    {
        // Arrange.
        var grain = NewApiKeyManagerGrain;

        // Act.
        var key = await grain.GenerateApiKeyAsync("name", "username");

        // Assert.
        key.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task IsApiKeyValid_ShouldValidateApiKey()
    {
        // Arrange.
        var grain = NewApiKeyManagerGrain;
        var key = await grain.GenerateApiKeyAsync("name", "username");

        // Act.
        var result = await grain.IsApiKeyValid(key);

        // Assert.
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsApiKeyValid_ShouldNotValidateApiKey_WhenApiKeyIsInvalid()
    {
        // Arrange.
        var grain = NewApiKeyManagerGrain;
        await grain.GenerateApiKeyAsync("name", "username");

        // Act.
        var result = await grain.IsApiKeyValid(_fixture.Create<string>());

        // Assert.
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteApiKey_ShouldDeleteApiKey()
    {
        // Arrange.
        var grain = NewApiKeyManagerGrain;
        var key = await grain.GenerateApiKeyAsync("name", "username");

        // Act.
        await grain.DeleteApiKeyAsync("name", "username");

        // Assert.
        var result = await grain.IsApiKeyValid(key);
        result.Should().BeFalse();
    }
}