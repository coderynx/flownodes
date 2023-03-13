using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk.Alerting;
using Flownodes.Shared.Interfaces;
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

    private IAlertManagerGrain ProvideAlertManagerGrain()
    {
        return _cluster.GrainFactory.GetGrain<IAlertManagerGrain>(_fixture.Create<string>());
    }

    [Fact]
    public async Task CreateAlert_ShouldCreateAlert()
    {
        var alertManager = ProvideAlertManagerGrain();

        var alert = await alertManager.CreateAlertAsync("tenant", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        alert.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlert_ShouldGetAlert()
    {
        var alertManager = ProvideAlertManagerGrain();

        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        var alert = await alertManager.GetAlert("tenant", "alert");
        alert.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlertByTargetObjectName_ShouldGetAlert()
    {
        var alertManager = ProvideAlertManagerGrain();

        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        var alert = await alertManager.GetAlertByTargetObjectName("tenant", "targetObject");
        alert.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlerts_ShouldGetAlerts()
    {
        var alertManager = ProvideAlertManagerGrain();

        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());
        await alertManager.CreateAlertAsync("tenant", "alert1", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        var alerts = await alertManager.GetAlerts("tenant");
        alerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveAlerts_ShouldRemoveAlerts()
    {
        var alertManager = ProvideAlertManagerGrain();

        await alertManager.CreateAlertAsync("tenant", "alert", "targetObject",
            AlertSeverity.Informational, "description", new HashSet<string>());

        await alertManager.RemoveAlertAsync("tenant", "alert");

        var alert = alertManager.GetAlert("tenant", "alert");
        alert.Should().NotBeNull();
    }
}