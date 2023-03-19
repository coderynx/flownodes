using Flownodes.ApiGateway.Mediator.Requests;
using Flownodes.ApiGateway.Mediator.Responses;
using Flownodes.Sdk;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Tenanting;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Handlers;

public class SearchResourcesByTagsHandler : IRequestHandler<SearchResourcesByTagsRequest, SearchResourceByTagsResponse>
{
    private readonly ITenantManagerGrain _tenantManager;

    public SearchResourcesByTagsHandler(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(FlownodesObjectNames.TenantManager);
    }

    public async Task<SearchResourceByTagsResponse> Handle(SearchResourcesByTagsRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantManager.GetTenantAsync(request.TenantName);
        if (tenant is null)
        {
            return new SearchResourceByTagsResponse(request.TenantName, request.Tags, "Tenant not found", ResponseKind.NotFound);
        }

        var resourceManager = await tenant.GetResourceManager();

        try
        {
            var resources = await resourceManager.SearchResourcesByTags(request.Tags);

            var searchResults = new HashSet<ResourceSearchResult>();
            foreach (var resource in resources)
            {
                var poco = await resource.GetSummary();
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