using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk;
using Flownodes.Sdk.Alerting;
using Flownodes.Shared.Alerting;
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

    private FlownodesId NewFlownodesId => new(FlownodesObject.AlertManager, _fixture.Create<string>());

    private IAlertManagerGrain NewAlertManagerGrain =>
        _cluster.GrainFactory.GetGrain<IAlertManagerGrain>(NewFlownodesId);

    [Fact]
    public async Task CreateAlert_ShouldCreateAlert()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;

        // Act.
        var alert = await alertManager.CreateAlertAsync("tenant", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        // Assert.
        alert.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlert_ShouldGetAlert()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;
        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        // Act.
        var alert = await alertManager.GetAlert("tenant", "alert");

        // Assert.
        alert.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlert_ShouldReturnNull_WhenAlertIsNotFound()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;
        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        // Act.
        var alert = await alertManager.GetAlert("tenant", "alert_1");

        // Assert.
        alert.Should().BeNull();
    }

    [Fact]
    public async Task GetAlertByTargetObjectName_ShouldReturnAlert()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;

        // Act.
        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        // Assert.
        var alert = await alertManager.GetAlertByTargetObjectName("tenant", "targetObject");
        alert.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlertByTargetObjectName_ShouldReturnNull_WhenAlertIsNotFound()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;

        // Act.
        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        // Assert.
        var alert = await alertManager.GetAlertByTargetObjectName("tenant", "targetObject_1");
        alert.Should().BeNull();
    }

    [Fact]
    public async Task GetAlerts_ShouldGetAlerts()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;

        // Act.
        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());
        await alertManager.CreateAlertAsync("tenant", "alert1", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        // Assert.
        var alerts = await alertManager.GetAlerts("tenant");
        alerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveAlert_ShouldRemoveAlert()
    {
        // Arrange.
        var alertManager = NewAlertManagerGrain;
        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        // Act.
        await alertManager.RemoveAlertAsync("tenant", "alert");

        // Assert.
        var alert = await alertManager.GetAlert("tenant", "alert");
        alert.Should().BeNull();
    }
}