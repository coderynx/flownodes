using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Tests.Configuration;
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
        _cluster = fixture.Cluster;
        _fixture = new Fixture();
    }

    [Fact]
    public async Task CreateWorkflow_ShouldCreateWorkflow()
    {
        // Arrange,
        var workflowManager = _cluster.GrainFactory.GetGrain<IWorkflowManagerGrain>(_fixture.Create<string>());

        // Act.
        var workflow = await workflowManager.CreateWorkflowAsync(_fixture.Create<string>(), _fixture.Create<string>());

        // Assert.
        workflow.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkflow_ShouldGetWorkflow()
    {
        // Arrange,
        var workflowManager = _cluster.GrainFactory.GetGrain<IWorkflowManagerGrain>(_fixture.Create<string>());

        var workflowName = _fixture.Create<string>();
        var workflow = await workflowManager.CreateWorkflowAsync(workflowName, _fixture.Create<string>());

        // Act.
        workflow = await workflowManager.GetWorkflowAsync(workflowName);

        // Assert.
        workflow.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkflows_ShouldGetWorkflows()
    {
        // Arrange,
        var workflowManager = _cluster.GrainFactory.GetGrain<IWorkflowManagerGrain>(_fixture.Create<string>());

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
        var workflowManager = _cluster.GrainFactory.GetGrain<IWorkflowManagerGrain>(_fixture.Create<string>());

        var workflowName = _fixture.Create<string>();
        var workflow = await workflowManager.CreateWorkflowAsync(workflowName, _fixture.Create<string>());

        // Act.
        await workflowManager.RemoveWorkflowAsync(workflowName);
        workflow = await workflowManager.GetWorkflowAsync(workflowName);

        // Assert.
        workflow.Should().BeNull();
    }
}