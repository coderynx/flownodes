using System;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Tests.Configuration;
using FluentAssertions;
using Orleans;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class AlerterGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public AlerterGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    private async Task<IAlertManagerGrain> ProvideAlerterAsync(params string[] drivers)
    {
        var grain = _cluster.GrainFactory.GetGrain<IAlertManagerGrain>(_fixture.Create<string>());
        await grain.SetupAsync(drivers);
        return grain;
    }

    [Fact]
    public void ShouldActivate()
    {
        // Arrange & act.
        var grain = _cluster.GrainFactory.GetGrain<IAlertManagerGrain>(_fixture.Create<string>());

        // Assert.
        grain.GetGrainId().Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldConfigureWithoutDriver()
    {
        // Arrange, Act & Assert.
        await this.Invoking(g => g.ProvideAlerterAsync()).Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task ShouldConfigureWithDriver()
    {
        // Arrange, Act & Assert.
        await this.Invoking(g => g.ProvideAlerterAsync("TestAlerterDriver")).Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task ShouldProduceAlert()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();

        // Act.
        var alert = await grain.FireInfoAsync(_fixture.Create<string>(), "Test");

        // Assert.
        alert.Should().NotBeNull();

        var alerts = await grain.GetAlerts();
        alerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldProduceAlert_WithDrivers()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync("TestAlerterDriver");

        // Act.
        var alert = await grain.FireInfoAsync(_fixture.Create<string>(), "Test");

        // Assert.
        alert.Should().NotBeNull();

        var alerts = await grain.GetAlerts();
        alerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldThrowOnProduceAlert_WhenFrnIsEmpty()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();

        // Act & Assert.
        var act = async () => { await grain.FireInfoAsync(string.Empty, "Test"); };
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ShouldThrowOnProduceAlert_WhenMessageIsEmpty()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();

        // Act & Assert.
        var act = async () => { await grain.FireInfoAsync(_fixture.Create<string>(), string.Empty); };
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ShouldClearAlerts()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();
        await grain.FireInfoAsync(_fixture.Create<string>(), "Test");

        // Act.
        await grain.ClearAlertsAsync();

        // Assert.
        var alerts = await grain.GetAlerts();
        alerts.Should().BeEmpty();
    }
}