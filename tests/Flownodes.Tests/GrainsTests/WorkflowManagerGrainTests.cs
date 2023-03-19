using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk;
using Flownodes.Shared.Workflows;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class WorkflowManagerGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public WorkflowManagerGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId NewId => new(FlownodesObject.WorkflowManager, _fixture.Create<string>());

    private IWorkflowManagerGrain NewWorkflowManagerGrain =>
        _cluster.GrainFactory.GetGrain<IWorkflowManagerGrain>(NewId);

    [Fact]
    public async Task CreateWorkflow_ShouldCreateWorkflow()
    {
        // Arrange,
        var workflowManager = NewWorkflowManagerGrain;

        // Act.
        var workflow = await workflowManager.CreateWorkflowAsync(_fixture.Create<string>(), _fixture.Create<string>());

        // Assert.
        workflow.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkflow_ShouldReturnWorkflow()
    {
        // Arrange,
        var workflowManager = NewWorkflowManagerGrain;

        var workflowName = _fixture.Create<string>();
        await workflowManager.CreateWorkflowAsync(workflowName, _fixture.Create<string>());

        // Act.
        var workflow = await workflowManager.GetWorkflowAsync(workflowName);

        // Assert.
        workflow.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkflows_ShouldReturnWorkflows()
    {
        // Arrange,
        var workflowManager = NewWorkflowManagerGrain;

        await workflowManager.CreateWorkflowAsync(_fixture.Create<string>(), _fixture.Create<string>());
        await workflowManager.CreateWorkflowAsync(_fixture.Create<string>(), _fixture.Create<string>());

        // Act.
        var workflows = await workflowManager.GetWorkflowsAsync();

        // Assert.
        workflows.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveWorkflow_ShouldRemoveWorkflow()
    {
        // Arrange.
        var workflowManager = NewWorkflowManagerGrain;

        var workflowName = _fixture.Create<string>();
        await workflowManager.CreateWorkflowAsync(workflowName, _fixture.Create<string>());

        // Act.
        await workflowManager.RemoveWorkflowAsync(workflowName);
        var workflow = await workflowManager.GetWorkflowAsync(workflowName);

        // Assert.
        workflow.Should().BeNull();
    }
}