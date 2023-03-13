using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Tests.Fixtures;
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

    // TODO: Add more relevant tests.

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteScriptWithoutThrowing()
    {
        var script = _cluster.GrainFactory.GetGrain<IScriptGrain>(GetFakeFlownodesId());

        const string code = """
        // #!/usr/local/bin/cscs
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Flownodes.Shared.Scripting;

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
        await script.ExecuteAsync();
    }
}