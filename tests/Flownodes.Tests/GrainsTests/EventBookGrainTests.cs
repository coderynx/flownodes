using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Eventing;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class EventBookGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public EventBookGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private EntityId NewEntityId => new(Entity.EventBook, _fixture.Create<string>());
    private IEventBookGrain NewEventBookGrain => _cluster.GrainFactory.GetGrain<IEventBookGrain>(NewEntityId);

    [Fact]
    public async Task RegisterEvent_ShouldRegisterEvent()
    {
        // Arrange.
        var grain = NewEventBookGrain;
        var id = new EntityId(Entity.ResourceManager, EntityNames.ResourceManager);

        // Act.
        var registration = await grain.RegisterEventAsync(EventKind.DeployedResource, id);

        // Assert.
        registration.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEvents_ShouldReturnEvents()
    {
        // Arrange.
        var grain = NewEventBookGrain;
        var id = new EntityId(Entity.ResourceManager, EntityNames.ResourceManager);
        await grain.RegisterEventAsync(EventKind.DeployedResource, id);

        // Act.
        var events = await grain.GetEvents();

        // Assert.
        events.Should().ContainSingle();
    }
}