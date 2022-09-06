using Refit;

namespace Flownodes.Components.OpenWeather.ApiSchemas;

public interface IOpenWeatherApi
{
    [Get("/data/2.5/weather")]
    Task<ApiResponse<string>> GetWeather([AliasAs("lat")] string latitude, [AliasAs("lon")] string longitude,
        [AliasAs("appid")] string applicationId);
}