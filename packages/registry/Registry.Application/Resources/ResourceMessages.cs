using Core.Contracts.Resources;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal static class ResourceMessages
{
    public static ResourceApplied ToApplied(this Resource resource) =>
        new(
            resource.Namespace.Value,
            resource.Kind,
            resource.Name.Value,
            resource.Uid,
            resource.Generation,
            resource.Seq,
            resource.Spec);

    public static ResourceDeleted ToDeleted(this Resource resource) =>
        new(
            resource.Namespace.Value,
            resource.Kind,
            resource.Name.Value,
            resource.Uid,
            resource.Generation,
            resource.Seq);
}
