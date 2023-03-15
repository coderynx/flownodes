using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk;
using Flownodes.Sdk.Alerting;
using Flownodes.Shared.Interfaces;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using OrleansCodeGen.Orleans.Serialization;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class AlertGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public AlertGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId ProvideFakeFlownodesId()
    {
        return new FlownodesId(FlownodesObject.Alert, _fixture.Create<string>(), _fixture.Create<string>());
    }
    
    [Fact]
    public async Task Initialize_ShouldInitializeAlert()
    {
        var alert = _cluster.GrainFactory.GetGrain<IAlertGrain>(ProvideFakeFlownodesId());

        await alert.InitializeAsync("target", DateTime.Now, AlertSeverity.Informational, "description",
            new HashSet<string> { "TestAlerterDriver" });

        var state = await alert.GetState();
        state.TargetObjectName.Should().NotBeNullOrEmpty();
        state.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Fire_ShouldFireAlert()
    {
        var alert = _cluster.GrainFactory.GetGrain<IAlertGrain>(ProvideFakeFlownodesId());

        await alert.InitializeAsync("target", DateTime.Now, AlertSeverity.Informational, "description",
            new HashSet<string> { "TestAlerterDriver" });

        await alert.FireAsync();
    }
}