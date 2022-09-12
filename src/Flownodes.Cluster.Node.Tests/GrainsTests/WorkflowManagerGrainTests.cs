using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Cluster.Core.Resources;
using Flownodes.Cluster.Node.Tests.Configuration;
using FluentAssertions;
using Orleans;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Cluster.Node.Tests.GrainsTests;

[Collection("TestCluster")]
public class WorkflowManagerGrainTests : IClassFixture<ClusterFixture>
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public WorkflowManagerGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    private static string GetTestWorkflowDefinition(string name)
    {
        return "{\"Id\": \"" + name +
               "\",\"Version\": 1,\"Steps\": [{\"Id\": \"LogHello\",\"StepType\": \"Flownodes.Edge.Node.Automation.LoggerStep, Flownodes.Edge.Node\"}]}";
    }

    [Fact]
    public void ShouldActivate()
    {
        // Arrange & act.
        var resourceManager = _cluster.GrainFactory.GetGrain<IWorkflowManagerGrain>(_fixture.Create<string>());

        // Assert.
        resourceManager.GetGrainIdentity().Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldLoadWorkflow()
    {
        // Arrange.
        var workflowId = _fixture.Create<string>();
        var workflowManager = _cluster.GrainFactory.GetGrain<IWorkflowManagerGrain>("workflow-manager");
        var workflowJson = GetTestWorkflowDefinition(workflowId);

        // Act.
        await workflowManager.LoadWorkflowAsync(workflowJson);
    }

    [Fact]
    public async Task ShouldStartWorkflow()
    {
        // Arrange.
        var workflowManager = _cluster.GrainFactory.GetGrain<IWorkflowManagerGrain>("workflow-manager");
        var workflowId = _fixture.Create<string>();
        var workflowJson = GetTestWorkflowDefinition(workflowId);
        await workflowManager.LoadWorkflowAsync(workflowJson);

        // Act.
        var id = await workflowManager.StartWorkflowAsync(workflowId);
        id.Should().NotBeNull();
    }
}