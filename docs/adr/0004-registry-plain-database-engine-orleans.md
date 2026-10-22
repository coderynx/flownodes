# Registry uses plain database access, Engine uses Orleans

The Registry stores Resources with plain database access (EF Core on Postgres) and no actors; the Engine is built on Microsoft Orleans, whose grains hold runtime state and talk to Devices and External Services. Desired state is a relational, queryable, versioned catalogue and gains nothing from actors, whereas live Connections, Devices, Assets and Flows are long-lived, concurrent and distributed, which is what Orleans is for.

## Consequences

- The Registry is the only place a Spec is written. The Engine writes only Status back.
- Each Engine grain persists a read-only copy of the last Spec it reconciled, so it keeps running on its last known configuration if the Registry is unavailable or a Silo restarts.
