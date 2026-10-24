# Module messages use Wolverine on its Postgres transport

Messages between modules (ADR-0012) go through Wolverine, using its Postgres transport, its EF Core transactional outbox and its durable inbox, so a Raspberry Pi needs no broker (ADR-0001) and a Resource message is published if and only if its apply commits. We chose it over MassTransit, whose v9 is commercial and whose v8 has no future; over CAP, which needs a broker for more than one node; over Orleans streams, whose Postgres provider is prerelease and would tie the Registry to Orleans; and over a hand-rolled outbox, whose retries, dead-lettering and leadership we would own forever.

## Consequences

- Wolverine is used only between modules. Commands and queries inside a module keep using `Coderynx.MediatorKit`.
- A single Silo runs Wolverine in Solo durability mode; a cluster elects a leader through a Postgres advisory lock, so connections must be direct or session-pooled, never transaction-pooled.
- Queue listeners wake on Postgres `LISTEN/NOTIFY` rather than waiting out Wolverine's 5-second idle poll, so a message reaches its module as soon as it commits. This holds one extra connection per module database; polling stays on as the fallback.
- Builds ship pre-generated handler code (`TypeLoadMode.Static`) so a Raspberry Pi never loads Roslyn at runtime.
- Wolverine does not order messages strictly, so ordering is the receiver's job (the per-Namespace `seq`, ADR-0014).
- An Engine grain cannot write its state and the outbox in one transaction, so messages from the Engine are at-least-once and may be lost relative to grain state; the Engine re-sends Status on resync to compensate.
