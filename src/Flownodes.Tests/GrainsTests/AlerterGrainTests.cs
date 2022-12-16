using System;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Core.Interfaces;
using Flownodes.Tests.Configuration;
using FluentAssertions;
using Orleans;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection(nameof(ClusterFixture))]
public class AlerterGrainTests : IClassFixture<ClusterFixture>
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public AlerterGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    private async Task<IAlerterGrain> ProvideAlerterAsync(params string[] drivers)
    {
        var grain = _cluster.GrainFactory.GetGrain<IAlerterGrain>(_fixture.Create<string>());
        await grain.ConfigureAsync(drivers);
        return grain;
    }

    [Fact]
    public void ShouldActivate()
    {
        // Arrange & act.
        var grain = _cluster.GrainFactory.GetGrain<IAlerterGrain>(_fixture.Create<string>());

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
    public async Task ShouldThrowOnProduceInfoAlert_WhenFrnOrMessageAreEmpty()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();

        // Act & Assert.
        await grain.Invoking(g => g.ProduceInfoAlertAsync(string.Empty, "Test")).Should()
            .ThrowAsync<ArgumentException>();
        await grain.Invoking(g => g.ProduceInfoAlertAsync(Globals.TestingFrn, string.Empty)).Should()
            .ThrowAsync<ArgumentException>();
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
    public async Task ShouldThrowOnProduceWarningAlert_WhenFrnOrMessageAreEmpty()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();

        // Act & Assert.
        await grain.Invoking(g => g.ProduceWarningAlertAsync(string.Empty, "Test")).Should()
            .ThrowAsync<ArgumentException>();
        await grain.Invoking(g => g.ProduceWarningAlertAsync(Globals.TestingFrn, string.Empty)).Should()
            .ThrowAsync<ArgumentException>();
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
    public async Task ShouldThrowOnProduceErrorAlert_WhenFrnOrMessageAreEmpty()
    {
        // Arrange.
        var grain = await ProvideAlerterAsync();

        // Act & Assert.
        await grain.Invoking(g => g.ProduceErrorAlertAsync(string.Empty, "Test")).Should()
            .ThrowAsync<ArgumentException>();
        await grain.Invoking(g => g.ProduceErrorAlertAsync(Globals.TestingFrn, string.Empty)).Should()
            .ThrowAsync<ArgumentException>();
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