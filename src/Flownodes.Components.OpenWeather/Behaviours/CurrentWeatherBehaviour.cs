using Ardalis.GuardClauses;
using Flownodes.Components.OpenWeather.ApiSchemas;
using Flownodes.Edge.Core.Resources;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Refit;

namespace Flownodes.Components.OpenWeather.Behaviours;

public class CurrentWeatherBehaviour : IDataCollectorBehaviour
{
    private const string WeatherBaseUrl = "https://api.openweathermap.org";
    private readonly string _token;

    public CurrentWeatherBehaviour(IConfiguration configuration)
    {
        _token = configuration["OpenWeather:Token"];
    }

    public async Task<object?> UpdateAsync(string actionId, Dictionary<string, object?> parameters)
    {
        var latitude = parameters["latitude"]?.ToString();
        Guard.Against.NullOrWhiteSpace(latitude, nameof(latitude));

        var longitude = parameters["longitude"]?.ToString();
        Guard.Against.NullOrWhiteSpace(longitude, nameof(longitude));

        Guard.Against.NullOrWhiteSpace(_token, nameof(_token));

        var openWeatherApi = RestService.For<IOpenWeatherApi>(WeatherBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer()
        });
        var response = await openWeatherApi.GetWeather(latitude, longitude, _token);

        if (!response.IsSuccessStatusCode) throw new Exception($"OpenWeather API returned {response.StatusCode}");

        Guard.Against.Null(response.Content);

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