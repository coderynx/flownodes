using System.Collections.Generic;
using System.Threading.Tasks;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing;
using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Resourcing;
using Flownodes.Worker.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Flownodes.Tests.Grains;

[GenerateSerializer]
internal sealed record TestResourceSummary
    (FlownodesId Id, Dictionary<string, object?> Metadata) : BaseResourceSummary(Id, Metadata);

internal sealed class TestResourceGrain : ResourceGrain, ITestResourceGrain
{
    public TestResourceGrain(ILogger<TestResourceGrain> logger, IEnvironmentService environmentService,
        IExtensionProvider extensionProvider, IPersistentStateFactory stateFactory, IGrainContext grainContext)
        : base(logger, environmentService, extensionProvider, stateFactory, grainContext)
    {
    }

    public ValueTask<BaseResourceSummary> GetSummary()
    {
        return ValueTask.FromResult<BaseResourceSummary>(new TestResourceSummary(Id, Metadata));
    }
}