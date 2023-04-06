namespace Flownodes.Worker.Mediator.Responses;

public sealed record ResourceSearchResult(string ResourceId, string TenantName, string ResourceName, string Kind);

public sealed record SearchResourceByTagsResponse : Response
{
    public SearchResourceByTagsResponse(string tenantName, HashSet<string> tags, HashSet<ResourceSearchResult> results)
    {
        TenantName = tenantName;
        Results = results;
        Tags = tags;
    }

    public SearchResourceByTagsResponse(string tenantName, HashSet<string> tags, string message,
        ResponseKind responseKind) :
        base(message, responseKind)
    {
        TenantName = tenantName;
        Tags = tags;
    }

    public string TenantName { get; }
    public HashSet<string> Tags { get; }
    public HashSet<ResourceSearchResult> Results { get; } = new();
}