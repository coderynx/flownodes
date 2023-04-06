using System.Text;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Attributes;
using Flownodes.Sdk.Resourcing.Behaviours;
using Microsoft.Extensions.Logging;

namespace Flownodes.Extensions.FakeDevice.Behaviours;

[BehaviourId("fake_device")]
[BehaviourDescription("Fake device behaviour for testing Flownodes")]
public class FakeDevice : IReadableDeviceBehaviour, IWritableDeviceBehaviour
{
    private readonly DeviceContext _context;
    private readonly ILogger<FakeDevice> _logger;
    private readonly Dictionary<string, object?> _state = new();

    private int _count;

    public FakeDevice(ILogger<FakeDevice> logger, DeviceContext context)
    {
        _logger = logger;
        _context = context;
    }

    public Task<UpdateResourceBag> OnSetupAsync()
    {
        _logger.LogInformation("Configured FakeDevice {@DeviceId}", _context.Id.ToString());
        return Task.FromResult(new UpdateResourceBag());
    }

    public Task<UpdateResourceBag> OnPullStateAsync()
    {
        var bag = new UpdateResourceBag { State = _state };
        if (_count % 5 is 0 || _count is 0) bag.State["fake_value"] = GenerateNewToken();

        _count++;

        _logger.LogInformation("Pulled state {@State} from FakeDevice {@DeviceId}", _state, _context.Id.ToString());
        return Task.FromResult(bag);
    }

    public Task OnPushStateAsync(Dictionary<string, object?> newState)
    {
        _logger.LogInformation("Pushed state to FakeDevice {@DeviceId}", _context.Id.ToString());
        return Task.CompletedTask;
    }

    private static string GenerateNewToken()
    {
        var rand = new Random();
        var randStr = "";
        while (randStr.Length <= 40) randStr += rand.Next(0, 255).ToString();

        var plainTextBytes = Encoding.UTF8.GetBytes(randStr);
        return Convert.ToBase64String(plainTextBytes);
    }
}