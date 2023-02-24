using System.Text.Json.Nodes;
using Refit;

namespace Flownodes.Ctl.ApiSchemas;

internal interface IResourceApi
{
    [Get("/api/tenants/{tenantName}/resources/{resourceName}")]
    Task<ApiResponse<JsonNode>> GetResourceAsync(string tenantName, string resourceName);

    [Get("/api/tenants/{tenantName}/resources")]
    Task<ApiResponse<JsonNode>> GetResourcesAsync(string tenantName);

    [Get("/api/tenants/{tenantName}/resources/search/{searchTerms}")]
    Task<ApiResponse<JsonNode>> SearchResourcesAsync(string tenantName, string searchTerms);
}