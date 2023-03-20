using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

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