using Flownodes.ApiGateway.Mediator.Responses;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Requests;

public sealed class SearchResourcesByTagsRequest : IRequest<SearchResourceByTagsResponse>
{
    public SearchResourcesByTagsRequest(string tenantName, HashSet<string> tags)
    {
        TenantName = tenantName;
        Tags = tags;
    }

    public string TenantName { get; }
    public HashSet<string> Tags { get; }
}