# Integrations run in-process with their own IoC container

Integrations are .NET assemblies loaded into the Silo and built against a versioned Flownodes SDK. Each Integration gets a dedicated IoC container that has no access to the host's container, so it sees only what the SDK deliberately hands it. We chose in-process over out-of-process plugins (separate processes or containers over gRPC) because out-of-process is too heavy for a Raspberry Pi deployment (see ADR-0001). The SDK is kept narrow so that Integrations can be moved behind a process boundary later without rewriting them.

## Consequences

- A faulty Integration can still crash or starve the Silo; isolation is of dependencies, not of failures.
- The container is not a security sandbox: an Integration can still read files, open sockets and reach the database. Installing an Integration means fully trusting its code, as with Home Assistant custom components. Real isolation would come from moving Integrations out of process.
- Because Integrations are written and released by third parties, the SDK is a public contract. It is versioned separately with semantic versioning, and each Flownodes release declares which SDK major versions it supports.
- Integrations contribute Devices, External Services, Data Points, Actions and Events, but not Node types. Node types are fixed by core; Flows reach Integrations through generic Action and Event Nodes.
- Integrations are external to Flownodes: they are released independently, and no Integration is bundled with or required by a Flownodes release.
- Integrations are installed with the deployment, identically on every Silo, and are only listed through the API. Installing them at runtime is a later feature.
