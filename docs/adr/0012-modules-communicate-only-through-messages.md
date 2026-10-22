# Modules communicate only through asynchronous messages

The Registry and the Engine, and any module added later, never call each other's code, never read or write each other's database, and never share tables. They exchange asynchronous messages whose contracts live in a shared contracts package, so that each module can later be detached into its own process without redesign. We chose this over in-process calls between modules of the monolith because a direct call or a shared table is a coupling that only shows up, and becomes expensive, the day the modules are split.

## Consequences

- Each module owns its own database. Clients (`flowctl`, the Web API, the web UI) still call each module's own HTTP endpoints synchronously; the rule is about module-to-module traffic.
- The Engine never reads a Spec from the Registry: every created or changed Resource is published with its whole Spec, and deletions are published too. Status travels back to the Registry the same way.
- The default transport is Postgres-backed, the same in the monolith and when detached, so a Raspberry Pi needs nothing beyond the database it already runs (ADR-0001). A broker can be added later behind the same abstraction.
- Messages can be duplicated or reordered, so every Resource message carries a per-Namespace sequence number and receivers drop anything not newer than what they last applied.
- Integration assemblies are loaded by both modules: the Registry runs their surface function and config validator at apply time (ADR-0009, ADR-0010) and the Engine runs their Connections. Detached modules need the same Integrations installed.
