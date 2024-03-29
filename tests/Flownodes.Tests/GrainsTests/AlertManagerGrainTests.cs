using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class AlertManagerGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public AlertManagerGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private EntityId NewEntityId => new(Entity.AlertManager, _fixture.Create<string>());

    private IAlertManagerGrain NewAlertManagerGrain =>
        _cluster.GrainFactory.GetGrain<IAlertManagerGrain>(NewEntityId);

    [Fact]
    public async Task CreateAlert_ShouldCreateAlert()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;

        // Act.
        var alert = await alertManager.CreateAlertAsync("targetObject", AlertSeverity.Informational, "description",
            new HashSet<string>());

        // Assert.
        alert.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlert_ShouldGetAlert()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;
        await alertManager.CreateAlertAsync("alert", "targetObject", AlertSeverity.Informational, "description",
            new HashSet<string>());

        // Act.
        var alert = await alertManager.GetAlert("alert");

        // Assert.
        alert.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlert_ShouldReturnNull_WhenAlertIsNotFound()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;
        await alertManager.CreateAlertAsync("alert", "targetObject", AlertSeverity.Informational, "description",
            new HashSet<string>());

        // Act.
        var alert = await alertManager.GetAlert("alert_1");

        // Assert.
        alert.Should().BeNull();
    }

    [Fact]
    public async Task GetAlertByTargetObjectName_ShouldReturnAlert()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;

        // Act.
        await alertManager.CreateAlertAsync("alert", "targetObject", AlertSeverity.Informational, "description",
            new HashSet<string>());

        // Assert.
        var alert = await alertManager.GetAlertByTargetObjectName("targetObject");
        alert.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlertByTargetObjectName_ShouldReturnNull_WhenAlertIsNotFound()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;

        // Act.
        await alertManager.CreateAlertAsync("alert", "targetObject", AlertSeverity.Informational, "description",
            new HashSet<string>());

        // Assert.
        var alert = await alertManager.GetAlertByTargetObjectName("targetObject_1");
        alert.Should().BeNull();
    }

    [Fact]
    public async Task GetAlerts_ShouldGetAlerts()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;

        // Act.
        await alertManager.CreateAlertAsync("alert", "targetObject", AlertSeverity.Informational, "description",
            new HashSet<string>());
        await alertManager.CreateAlertAsync("alert1", "targetObject", AlertSeverity.Informational, "description",
            new HashSet<string>());

        // Assert.
        var alerts = await alertManager.GetAlerts();
        alerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveAlert_ShouldRemoveAlert()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;
        await alertManager.CreateAlertAsync("alert", "targetObject", AlertSeverity.Informational, "description",
            new HashSet<string>());

        // Act.
        await alertManager.RemoveAlertAsync("alert");

        // Assert.
        var alert = await alertManager.GetAlert("alert");
        alert.Should().BeNull();
    }
}