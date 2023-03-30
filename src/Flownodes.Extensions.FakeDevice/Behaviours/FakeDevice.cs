using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Attributes;
using Flownodes.Sdk.Resourcing.Behaviours;
using Microsoft.Extensions.Logging;

namespace Flownodes.Extensions.FakeDevice.Behaviours;

[BehaviourId("fake_device")]
[BehaviourDescription("Fake device behaviour for testing Flownodes")]
public class FakeDevice : IReadableDeviceBehaviour, IWritableDeviceBehaviour
{
    public FakeDevice(ILogger<FakeDevice> logger)
    {
        _logger = logger;
    }

    private readonly ILogger<FakeDevice> _logger;

    public Task OnSetupAsync(ResourceContext context)
    {
        _logger.LogInformation("Configured FakeDevice {@DeviceId}", context.Id.ToString());
        return Task.CompletedTask;
    }

    public Task OnPullStateAsync(ResourceContext context)
    {
        context.State["fake_value"] = FakeValueGenerator();
        context.State["pulled_at"] = DateTime.Now;
        
        _logger.LogInformation("Pulled state from FakeDevice {@DeviceId}", context.Id.ToString());
        return Task.CompletedTask;
    }

    public Task OnPushStateAsync(Dictionary<string, object?> newState, ResourceContext context)
    {
        _logger.LogInformation("Pushed state to FakeDevice {@DeviceId}", context.Id.ToString());
        return Task.CompletedTask;
    }

    private int _count;
    private string _fakeValue = Guid.NewGuid().ToString();
    
    private string FakeValueGenerator()
    {
        if (_count % 10 is 0) _fakeValue = Guid.NewGuid().ToString();
        
        _count++;
        return _fakeValue;
    }
}