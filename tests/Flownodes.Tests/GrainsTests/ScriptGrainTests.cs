using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Sdk;
using Flownodes.Shared.Resourcing.Scripts;
using Flownodes.Tests.Fixtures;
using FluentAssertions;
using Orleans.TestingHost;
using Xunit;

namespace Flownodes.Tests.GrainsTests;

[Collection("TestCluster")]
public class ScriptGrainTests
{
    private readonly TestCluster _cluster;
    private readonly IFixture _fixture;

    public ScriptGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster!;
        _fixture = new Fixture();
    }

    private FlownodesId GetFakeFlownodesId()
    {
        return new FlownodesId(FlownodesObject.Script, _fixture.Create<string>(), _fixture.Create<string>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteScriptWithoutThrowing()
    {
        // Arrange.
        var script = _cluster.GrainFactory.GetGrain<IScriptGrain>(GetFakeFlownodesId());
        const string code = """
        // #!/usr/local/bin/cscs
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Flownodes.Shared.Resourcing.Scripts;

        public class TestScript : IScript
        {
            public FlownodesContext Context { get; set; }

            public async Task ExecuteAsync(Dictionary<string, object?> parameters)
            {
                Context.LogInformation("Hello");
            }
        }
        """;
        var configuration = new Dictionary<string, object?>
        {
            { "code", code }
        };
        await script.UpdateConfigurationAsync(configuration);

        // Act & Assert.
        var act = async () => { await script.ExecuteAsync(); };
        await act.Should().NotThrowAsync();
    }
}