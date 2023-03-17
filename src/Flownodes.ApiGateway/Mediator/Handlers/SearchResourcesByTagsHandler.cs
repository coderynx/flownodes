using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Sdk;
using Flownodes.Shared.Interfaces;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class SearchResourcesByTagsHandler : IRequestHandler<SearchResourcesByTagsRequest, SearchResourceByTagsResponse>
{
    private readonly IResourceManagerGrain _resourceManager;

    private readonly ITenantManagerGrain _tenantManager;

    public SearchResourcesByTagsHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManager);
        _resourceManager = grainFactory.GetGrain<IResourceManagerGrain>(FlownodesObjectNames.ResourceManager);
    }

    public async Task<SearchResourceByTagsResponse> Handle(SearchResourcesByTagsRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _tenantManager.IsTenantRegistered(request.TenantName))
            return new SearchResourceByTagsResponse(request.TenantName, request.Tags, "Tenant not found",
                ResponseKind.NotFound);

        try
        {
            var resources = await _resourceManager.SearchResourcesByTags(request.TenantName, request.Tags);

            var searchResults = new HashSet<ResourceSearchResult>();
            foreach (var resource in resources)
            {
                var poco = await resource.GetPoco();
                var result = new ResourceSearchResult(poco.Id, poco.Id.FirstName, poco.Id.SecondName!,
                    poco.Id.ToObjectKindString(),
                    poco.BehaviorId);
                searchResults.Add(result);
            }

            return new SearchResourceByTagsResponse(request.TenantName, request.Tags, searchResults);
        }
        catch
        {
            return new SearchResourceByTagsResponse(request.TenantName, request.Tags,
                "Could not execute query on cluster", ResponseKind.InternalError);
        }
    }
}