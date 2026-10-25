using Core.Contracts.Resources;
using Registry.Domain.Namespaces;

namespace Registry.Application.Namespaces;

internal static class NamespaceMessages
{
    public static ResourceApplied ToApplied(this Namespace @namespace) =>
        new(
            @namespace.Name.Value,
            Namespace.Kind,
            @namespace.Name.Value,
            @namespace.Uid,
            @namespace.Generation,
            @namespace.Seq,
            Namespace.Spec);

    public static ResourceDeleted ToDeleted(this Namespace @namespace) =>
        new(
            @namespace.Name.Value,
            Namespace.Kind,
            @namespace.Name.Value,
            @namespace.Uid,
            @namespace.Generation,
            @namespace.Seq);
}
