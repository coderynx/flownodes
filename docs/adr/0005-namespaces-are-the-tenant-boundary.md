# Namespaces are the tenant boundary

One Flownodes installation can serve several tenants, each isolated in its own Namespace. Unlike Kubernetes namespaces, a Flownodes Namespace is a hard isolation boundary: Resources never reference across Namespaces, there is no separate Tenant concept above it, and grouping inside a Namespace is the Asset tree's job. Namespaces share Registry tables and Engine grain storage, with the Namespace part of every Resource's identity and every grain key; a schema or database per Namespace was rejected as too heavy for a Raspberry Pi (ADR-0001) and awkward for Orleans storage.

## Consequences

- Authentication and per-Namespace roles (Operator, Engineer) are in v1 scope, plus an installation-wide Administrator. Without them Namespaces would isolate nothing.
- Integrations and Namespaces themselves are installation-wide; every other Kind is namespaced, including Asset Types.
- Two tenants sharing one physical broker each declare their own Connection to it.
- A fresh installation creates a `default` Namespace and makes the first user its Administrator and an Engineer and Operator in it, so a single-person home install never has to think about tenancy.
- Isolation is logical only in v1: there are no resource quotas, so one Namespace can starve another.
