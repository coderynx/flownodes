using Flownodes.Components.PhilipsHue.Models;
using Refit;

namespace Flownodes.Components.PhilipsHue.ApiSchemas;

internal interface IHueBridgeApi
{
    [Get("/api/{apiKey}/config")]
    Task<ApiResponse<HueBridgeConfiguration>> GetConfiguration(string apiKey);

    [Get("/api/{apiKey}/lights/{id}")]
    Task<ApiResponse<HueBridgeConfiguration>> GetLight(string apiKey, string id);

    [Put("/api/{apiKey}/lights/{id}/state")]
    Task<ApiResponse<string>> UpdateHueLightState(string apiKey, string id, [Body] string state);
}