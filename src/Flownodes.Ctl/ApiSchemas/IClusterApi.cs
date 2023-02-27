using System.Text.Json.Nodes;
using Refit;

namespace Flownodes.Ctl.ApiSchemas;

public interface IClusterApi
{
    [Get("/api/cluster")]
    Task<ApiResponse<JsonNode>> GetInfoAsync();
}