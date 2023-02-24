using Flownodes.Sdk.Resourcing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWeatherMap.Standard;

namespace Flownodes.Components.OpenWeatherMap;

[BehaviourId("open_weather_map")]
[BehaviourDescription("OpenWeatherMap data source behaviour for Flownodes")]
public class OpenWeatherMapSource : BaseDataSource
{
    private readonly Current _currentWeather;

    private readonly ILogger<OpenWeatherMapSource> _logger;

    public OpenWeatherMapSource(ILogger<OpenWeatherMapSource> logger, IConfiguration configuration) : base(logger)
    {
        _logger = logger;
        _currentWeather = new Current(configuration["OpenWeatherMap:Token"]);
    }

    public override async ValueTask<object?> GetDataAsync(string actionId,
        Dictionary<string, object?>? parameters = null)
    {
        if (parameters is null) return default;

        switch (actionId)
        {
            case "get_current_by_city":
                if (!parameters.TryGetValue("city", out var city)) return default;
                var weather = await _currentWeather.GetWeatherDataByCityName(city as string);
                return weather;

            // TODO: Continue implementation.
        }

        _logger.LogInformation("Received data from OpenWeatherMap");
        return default;
    }
}