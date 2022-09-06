using Flownodes.Components.OpenWeather.ApiSchemas;
using Flownodes.Edge.Core.Resources;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Refit;
using Throw;

namespace Flownodes.Components.OpenWeather.Behaviors;

public class CurrentWeatherBehavior : IDataCollectorBehavior
{
    private const string WeatherBaseUrl = "https://api.openweathermap.org";
    private readonly string _token;

    public CurrentWeatherBehavior(IConfiguration configuration)
    {
        _token = configuration["OpenWeather:Token"];
    }

    public async Task<object?> UpdateAsync(string actionId, Dictionary<string, object?> parameters)
    {
        var latitude = parameters["latitude"]?.ToString();
        latitude.Throw().IfNullOrWhiteSpace(_ => latitude);

        var longitude = parameters["longitude"]?.ToString();
        longitude.Throw().IfNullOrWhiteSpace(_ => longitude);

        _token.Throw().IfNullOrWhiteSpace(_ => _token);

        var openWeatherApi = RestService.For<IOpenWeatherApi>(WeatherBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer()
        });
        var response = await openWeatherApi.GetWeather(latitude, longitude, _token);

        response.IsSuccessStatusCode.Throw().IfFalse();
        response.Content.ThrowIfNull();

        var obj = JObject.Parse(response.Content);

        return new
        {
            baseName = obj["base"],
            city = obj["name"],
            timezone = obj["timezone"],
            main = obj["weather"]?[0]?["main"],
            description = obj["weather"]?[0]?["description"],
            temperature = obj["main"]?["temp"]
        };
    }
}