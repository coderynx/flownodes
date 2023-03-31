using System.Text.Json;
using Carter;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Mediator.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Flownodes.Worker.Routes;

internal class TagsQuery
{
    public string[] Values { get; private init; } = Array.Empty<string>();

    public static bool TryParse(string? value, out TagsQuery result)
    {
        result = new TagsQuery
        {
            Values = value?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()
        };

        return true;
    }
}

public class ResourceModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/tenants/{tenantName}/resources/{resourceName}",
                async ([FromServices] IMediator mediator, string tenantName, string resourceName) =>
                {
                    var request = new GetResourceRequest(tenantName, resourceName);
                    var response = await mediator.Send(request);

                    return response.GetResult();
                })
            .WithName("GetResource")
            .WithDisplayName("Get resource");

        app.MapGet("api/tenants/{tenantName}/resources", async ([FromServices] IMediator mediator, string tenantName) =>
            {
                var request = new GetResourcesRequest(tenantName);
                var response = await mediator.Send(request);

                return response.GetResult();
            })
            .WithName("GetResources")
            .WithDisplayName("Get resources");

        app.MapPut("api/tenants/{tenantName}/resources/{resourceName}/state",
                async ([FromServices] IMediator mediator, string tenantName, string resourceName,
                    JsonElement state) =>
                {
                    var dictionary = new Dictionary<string, object?>();

                    foreach (var property in state.EnumerateObject())
                    {
                        switch (property.Value.ValueKind)
                        {
                            case JsonValueKind.String:
                                dictionary.Add(property.Name, property.Value.GetString());
                                break;
                            case JsonValueKind.Number:
                                dictionary.Add(property.Name, property.Value.GetDouble());
                                break;
                            case JsonValueKind.True:
                                dictionary.Add(property.Name, true);
                                break;
                            case JsonValueKind.False:
                                dictionary.Add(property.Name, false);
                                break;
                            case JsonValueKind.Array:
                                dictionary.Add(property.Name, property.Value.EnumerateArray().Select(x => x.ToString()).ToArray());
                                break;
                            case JsonValueKind.Undefined:
                                break;
                            case JsonValueKind.Object:
                                break;
                            case JsonValueKind.Null:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    
                    var request = new UpdateResourceStateRequest(tenantName, resourceName, dictionary);
                    var response = await mediator.Send(request);

                    return response.GetResult();
                })
            .WithName("UpdateResourceState")
            .WithDisplayName("Update resource state");

        app.MapGet("api/tenants/{tenantName}/resources/search/{tags}",
                async ([FromServices] IMediator mediator, string tenantName, TagsQuery tags) =>
                {
                    var request = new SearchResourcesByTagsRequest(tenantName, tags.Values.ToHashSet());
                    var response = await mediator.Send(request);

                    return response.GetResult();
                })
            .WithName("SearchResourcesByTags")
            .WithDisplayName("Search resources by tags");

        app.MapGet("api/tenants/{tenantName}/resources/groups/{resourceGroupName}",
            async ([FromServices] IMediator mediator, string tenantName, string resourceGroupName) =>
            {
                var request = new GetResourceGroupRequest(tenantName, resourceGroupName);
                var response = await mediator.Send(request);

                return response.GetResult();
            });
    }
}