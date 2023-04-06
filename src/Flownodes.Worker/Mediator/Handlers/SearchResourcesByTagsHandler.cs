using Flownodes.Worker.Mediator.Requests;
using Flownodes.Worker.Mediator.Responses;
using Flownodes.Worker.Services;
using MediatR;

namespace Flownodes.Worker.Mediator.Handlers;

public class SearchResourcesByTagsHandler : IRequestHandler<SearchResourcesByTagsRequest, SearchResourceByTagsResponse>
{
    private readonly IEnvironmentService _environmentService;

    public SearchResourcesByTagsHandler(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    public async Task<SearchResourceByTagsResponse> Handle(SearchResourcesByTagsRequest request,
        CancellationToken cancellationToken)
    {
        var resourceManager = await _environmentService.GetResourceManager(request.TenantName);
        if (resourceManager is null)
            return new SearchResourceByTagsResponse(request.TenantName, request.Tags, "Tenant not found",
                ResponseKind.NotFound);

        try
        {
            var resources = await resourceManager.SearchResourcesByTags(request.Tags);

            var searchResults = new HashSet<ResourceSearchResult>();
            foreach (var resource in resources)
            {
                var poco = await resource.GetSummary();
                var result = new ResourceSearchResult(poco.Id, poco.Id.FirstName, poco.Id.SecondName!,
                    poco.Id.ToEntityKindString());
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