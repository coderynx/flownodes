using Microsoft.Extensions.Logging;

namespace Flownodes.Shared.Interfaces;

public class ScriptContext
{
    private readonly ILogger<ScriptContext> _logger;
    public readonly IResourceManagerGrain ResourceManager;

    public ScriptContext(ILogger<ScriptContext> logger, IResourceManagerGrain resourceManager)
    {
        _logger = logger;
        ResourceManager = resourceManager;
    }

    public async Task<IDeviceGrain?> GetDevice(string id)
    {
        return await ResourceManager.GetResourceAsync<IDeviceGrain>(id);
    }

    public void LogInformation(string text)
    {
        _logger.LogInformation("{Message}", text);
    }

    public void LogWarning(string text)
    {
        _logger.LogWarning("{Message}", text);
    }

    public void LogError(string text)
    {
        _logger.LogError("{Message}", text);
    }

    public void LogCritical(string text)
    {
        _logger.LogCritical("{Message}", text);
    }
}