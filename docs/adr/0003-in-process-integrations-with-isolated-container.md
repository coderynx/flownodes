# Integrations run in-process with their own IoC container

Integrations are .NET assemblies loaded into the Silo and built against a versioned Flownodes SDK. Each Integration gets a dedicated IoC container that has no access to the host's container, so it sees only what the SDK deliberately hands it. We chose in-process over out-of-process plugins (separate processes or containers over gRPC) because out-of-process is too heavy for a Raspberry Pi deployment (see ADR-0001). The SDK is kept narrow so that Integrations can be moved behind a process boundary later without rewriting them.

## Consequences

- A faulty Integration can still crash or starve the Silo; isolation is of dependencies, not of failures.
- The container is not a security sandbox: an Integration can still read files, open sockets and reach the database. Installing an Integration means fully trusting its code, as with Home Assistant custom components. Real isolation would come from moving Integrations out of process.
- Because Integrations are written and released by third parties, the SDK is a public contract. It is versioned separately with semantic versioning, and each Flownodes release declares which SDK major versions it supports.
- Integrations contribute one kind of Connection and Device Types, with their Data Points, Actions and Events (ADR-0010, ADR-0011), but not Node types. Node types are fixed by core; Flows reach Integrations through generic Action and Event Nodes.
- Integrations are external to Flownodes: they are released independently, and no Integration is bundled with or required by a Flownodes release.
- Integrations are installed with the deployment, identically on every Silo, and are only listed through the API. Installing them at runtime is a later feature.
- Each Integration is a folder of `dotnet publish` output in the deployment's integrations directory, with its entry assembly marked by an assembly attribute. It declares a vendor-qualified id, such as `flownodes.mqtt`, which Connections reference without a version. Two folders claiming one id both fail to load, and a load failure never stops the Silo.
- Each Integration has its own AssemblyLoadContext. Only the framework, the SDK and the logging and dependency-injection abstractions are shared with the host; every other dependency is private to the Integration. The host pre-registers only SDK services in its container.
- The SDK is `Flownodes.Sdk`, built in this repository. At load time the host checks the SDK version an Integration was built against and refuses unsupported ones; while the SDK is `0.x`, the minor version must match exactly.
- Integration code never runs on the Orleans scheduler. A Connection runs on exactly one Silo at a time and is restarted elsewhere on failover, so Integrations must not assume a Silo. Values, Events, Discoveries and connectivity flow out through a host-provided sink, and the host restarts a failed Connection with backoff.
- Every Action call carries an invocation id derived from the Flow Execution, the Node and the Node's visit count, so a call repeated after failover (ADR-0006) has the same id and an Integration can deduplicate on it.
- Runtime credentials such as pairing tokens live in a per-Connection state store the SDK provides, encrypted at rest and discarded with the Connection.
