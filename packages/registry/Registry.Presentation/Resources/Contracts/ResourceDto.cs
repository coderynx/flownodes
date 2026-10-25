using System.Text.Json;
using System.Text.Json.Nodes;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Presentation.Resources.Contracts;

internal sealed record ResourceDto(
    string ApiVersion,
    string Kind,
    ResourceMetadataDto Metadata,
    JsonElement Spec,
    JsonObject? Status);

internal sealed record ResourceMetadataDto(string Name, string? Namespace, long Generation, Guid Uid);

internal sealed record RevisionDto(
    string Kind,
    string Name,
    Guid Uid,
    long Generation,
    bool Deleted,
    RevisionAuthorDto AppliedBy,
    DateTimeOffset AppliedAt,
    JsonElement? Spec);

internal sealed record RevisionAuthorDto(Guid UserId, string? Username);

internal static class ResourceMappings
{
    public static ResourceDto ToDto(this Resource resource) =>
        new(
            Resource.ApiVersion,
            resource.Kind,
            new ResourceMetadataDto(resource.Name.Value, resource.Namespace.Value, resource.Generation, resource.Uid),
            Shown(resource.Kind, resource.Spec),
            Status(resource));

    public static ResourceDto ToDto(this Namespace @namespace) =>
        new(
            Resource.ApiVersion,
            Namespace.Kind,
            new ResourceMetadataDto(@namespace.Name.Value, null, @namespace.Generation, @namespace.Uid),
            Namespace.Spec,
            null);

    public static RevisionDto ToDto(this Revision revision) =>
        new(
            revision.Kind,
            revision.Name.Value,
            revision.Uid,
            revision.Generation,
            revision.Deleted,
            new RevisionAuthorDto(revision.Author.UserId, revision.Author.Username),
            revision.AppliedAt,
            revision.Spec is { } spec ? Shown(revision.Kind, spec) : null);

    private static JsonElement Shown(string kind, JsonElement spec) =>
        kind == SecretKind.KindName ? SecretKind.Redact(spec) : spec;

    private static JsonObject? Status(Resource resource)
    {
        if (resource.Status is not { } status)
        {
            return null;
        }

        var result = new JsonObject
        {
            ["observedGeneration"] = resource.ObservedGeneration
        };

        foreach (var field in status.EnumerateObject().Where(field => field.Name != "observedGeneration"))
        {
            result[field.Name] = JsonNode.Parse(field.Value.GetRawText());
        }

        return result;
    }
}
