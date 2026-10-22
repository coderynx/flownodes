# Devices exist only once declared

A Device is a Resource that exists only when an Engineer declares it, binding it to a Connection and an Integration-defined address. What a Connection can reach but nobody has declared is a Discovery, listed in the Connection's Status. Likewise, credentials an Integration obtains at runtime, such as a token from pressing a Philips Hue bridge's pairing button, are runtime state of the Connection held by the Engine, not Secrets. We chose this over letting the Engine create Device Resources or write Secrets because it keeps the Registry the only writer of Specs (ADR-0004) and keeps every Spec authored by a person and reproducible from Manifests.

## Consequences

- A Device can be declared before it has ever been discovered, for example for a train that is not online yet; its Status reports that it has not been found.
- Rebuilding an installation from its Manifests means pairing again with Integrations that pair at runtime. Connections that need pairing report it in their Status, and pairing is an Action on the Connection.
- Attaching a Device to an Asset is part of the Device's Spec, so "attached to at most one Asset" holds by construction.
