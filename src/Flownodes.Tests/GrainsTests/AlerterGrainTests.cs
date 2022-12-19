using System;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Core.Interfaces;
using Flownodes.Tests.Configuration;
using FluentAssertions;
using NSubstitute.Core;
using Orleans;
using Orleans.TestingHost;
using Throw;
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

    private async Task<IAlerManagerGrain> ProvideAlerterAsync(params string[] drivers)
    {
        var grain = _cluster.GrainFactory.GetGrain<IAlerManagerGrain>(_fixture.Create<string>());
        await grain.ConfigureAsync(drivers);
        return grain;
    }

    [Fact]
    public void ShouldActivate()
    {
        // Arrange & act.
        var grain = _cluster.GrainFactory.GetGrain<IAlerManagerGrain>(_fixture.Create<string>());

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
    public async Task ShouldProduceInfoAlert()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();

        // Act.
        var alert = await grain.ProduceInfoAlertAsync(Globals.TestingFrn, "Test");

        // Assert.
        alert.Should().NotBeNull();

        var alerts = await grain.GetAlerts();
        alerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldProduceInfoAlert_WithDrivers()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync("TestAlerterDriver");

        // Act.
        var alert = await grain.ProduceInfoAlertAsync(Globals.TestingFrn, "Test");

        // Assert.
        alert.Should().NotBeNull();

        var alerts = await grain.GetAlerts();
        alerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldThrowOnProduceAlert_WhenFrnIsWhitespace()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();
        
        // Act & Assert.
        var act = async () =>
        {
            await grain.ProduceInfoAlertAsync(" ", "Test");
        };
        await act.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task ShouldThrowOnProduceAlert_WhenMessageIsWhitespace()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();
        
        // Act & Assert.
        var act = async () =>
        {
            await grain.ProduceInfoAlertAsync(_fixture.Create<string>(), " ");
        };
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ShouldProduceWarningAlert()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();

        // Act.
        var alert = await grain.ProduceWarningAlertAsync(Globals.TestingFrn, "Test");

        // Assert.
        alert.Should().NotBeNull();

        var alerts = await grain.GetAlerts();
        alerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldProduceWarningAlert_WithDrivers()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync("TestAlerterDriver");

        // Act.
        var alert = await grain.ProduceWarningAlertAsync(Globals.TestingFrn, "Test");

        // Assert.
        alert.Should().NotBeNull();

        var alerts = await grain.GetAlerts();
        alerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldProduceErrorAlert()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();

        // Act.
        var alert = await grain.ProduceErrorAlertAsync(Globals.TestingFrn, "Test");

        // Assert.
        alert.Should().NotBeNull();

        var alerts = await grain.GetAlerts();
        alerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldProduceErrorAlert_WithDrivers()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync("TestAlerterDriver");

        // Act.
        var alert = await grain.ProduceErrorAlertAsync(Globals.TestingFrn, "Test");

        // Assert.
        alert.Should().NotBeNull();

        var alerts = await grain.GetAlerts();
        alerts.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task ShouldClearAlerts()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();
        await grain.ProduceInfoAlertAsync(Globals.TestingFrn, "Test");

        // Act.
        await grain.ClearAlertsAsync();

        // Assert.
        var alerts = await grain.GetAlerts();
        alerts.Should().BeEmpty();
    }
}