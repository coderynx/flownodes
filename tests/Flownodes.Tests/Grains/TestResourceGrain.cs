using System.Collections.Generic;
using System.Threading.Tasks;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Tests.Interfaces;
using Flownodes.Worker.Resourcing;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Flownodes.Tests.Grains;

internal sealed class TestResourceGrain : ResourceGrain, ITestResourceGrain
{
    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _configuration;
    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _state;

    public TestResourceGrain(ILogger<TestResourceGrain> logger, IPersistentStateFactory stateFactory,
        IGrainContext grainContext)
        : base(logger, stateFactory, grainContext)
    {
        _configuration =
            GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_configuration");
        _state = GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_state");
    }

    public ValueTask<ResourceSummary> GetSummary()
    {
        return ValueTask.FromResult(new ResourceSummary(Id, Metadata.State, new Dictionary<string, object?>()));
    }

    public async ValueTask<Dictionary<string, object?>> GetConfiguration()
    {
        return await _configuration.Get();
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?> configuration)
    {
        await _configuration.UpdateAsync(configuration);
    }

    public async Task ClearConfigurationAsync()
    {
        await _configuration.ClearAsync();
    }

    public async Task UpdateStateAsync(Dictionary<string, object?> state)
    {
        await _state.UpdateAsync(state);
    }

    public async ValueTask<Dictionary<string, object?>> GetState()
    {
        return await _state.Get();
    }

    public async Task ClearStateAsync()
    {
        await _state.ClearAsync();
    }
}