# Integration configuration is validated at apply time

The Integration-specific part of a Connection's Spec is validated by the Registry, at apply time, against a schema the installed Integration publishes through the SDK. We chose this over accepting it unchecked and letting the Engine report problems in Status so that a typo in a broker address or a camera setting fails at `apply`, like every other validation error (ADR-0007), rather than surfacing later as a Connection that will not connect.

## Consequences

- The Registry depends on the set of installed Integrations: a Connection naming an Integration that is not installed is rejected.
- A stored Spec can become invalid when an Integration is upgraded or removed. The Registry keeps it unchanged and the Engine reports it in the Connection's Status without reconciling it; the next apply is validated against the new schema.
