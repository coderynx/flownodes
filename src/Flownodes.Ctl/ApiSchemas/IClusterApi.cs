using System.Text.Json.Nodes;
using Refit;

namespace Flownodes.Ctl.ApiSchemas;

public interface IClusterApi
{
    [Get("/api/cluster")]
    Task<ApiResponse<JsonNode>> GetClusterInfoAsync();

    [Get("/api/resources/{id}")]
    Task<ApiResponse<JsonNode>> GetResourceSummaryAsync(string id);

    [Get("/api/resources")]
    Task<ApiResponse<JsonNode>> GetResourceSummariesAsync();
}