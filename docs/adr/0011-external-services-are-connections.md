# External Services are Connections, not Resources

An External Service, such as a weather API or a ticketing system, is not a Kind of its own: its Actions and Events belong to the Connection that reaches it, derived from the Connection's Spec in the same way a Device's surface is (ADR-0010). Connections already needed Actions of their own, such as pairing with a Hue bridge (ADR-0008), so a separate `ExternalService` Kind declared on a Connection would add a Resource, a reference and a delete ordering without letting an Engineer express anything new for the common case of one account per Connection.

## Consequences

- A Flow invokes an External Service's Actions and subscribes to its Events through the Connection.
- A Connection that reaches several distinct services, such as several projects in one ticketing account, exposes them as parameters of its Actions rather than as separately declared things. If that proves too coarse, a Kind can be added later.
- External Services still never have Data Points and are never Devices; their data reaches Assets only through Flows.
