using Flownodes.Sdk.Resourcing.Attributes;
using Flownodes.Sdk.Resourcing.Behaviours;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWeatherMap.Standard;

namespace Flownodes.Extensions.OpenWeatherMap.Behaviours;

[BehaviourId("open_weather_map")]
[BehaviourDescription("OpenWeatherMap data source behaviour for Flownodes")]
public class OpenWeatherMap : IDataSourceBehaviour
{
    private readonly Current _currentWeather;
    private readonly ILogger<OpenWeatherMap> _logger;

    public OpenWeatherMap(ILogger<OpenWeatherMap> logger, IConfiguration configuration)
    {
        _logger = logger;

        var token = configuration["OpenWeatherMap:Token"] ??
                    throw new InvalidOperationException("OpenWeatherMap token not found");
        _currentWeather = new Current(token);
    }

    public async ValueTask<object?> GetDataAsync(string actionId, Dictionary<string, object?>? parameters = null)
    {
        if (parameters is null) return default;

        switch (actionId)
        {
            case "get_current_by_city":
                if (!parameters.TryGetValue("city", out var city)) return default;
                return await _currentWeather.GetWeatherDataByCityName(city as string);
        }

        _logger.LogInformation("Pulled data from OpenWeatherMap");
        return default;
    }
}