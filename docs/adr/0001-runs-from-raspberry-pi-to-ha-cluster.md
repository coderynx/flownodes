# Flownodes runs both on a single Raspberry Pi and as an HA cluster

Flownodes must be deployable as a single Silo on a Raspberry Pi and as a multi-Silo high-availability cluster, from the same codebase and the same feature set. Every dependency and design choice has to work at both ends: nothing may require hardware, memory or external infrastructure that a Raspberry Pi cannot reasonably host, and nothing may assume a single Silo.

Flownodes is also local-first at both ends: it never requires a cloud service or an internet connection to run, and External Services are always optional.

## Consequences

- Core Flownodes is not a long-term historian. It keeps the current value and a bounded short-term history of each Data Point; long-term storage goes through an Integration to an external time-series database.
- A Raspberry Pi deployment supports Namespaces (ADR-0005) but is effectively single-tenant in practice, since v1 has no resource quotas.
- Safety-critical control is out of scope: Flownodes supervises and automates, but does not replace PLC-level safety logic.
