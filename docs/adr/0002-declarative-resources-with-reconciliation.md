# Configuration is declarative Resources, continuously reconciled

All configuration (Connections, Asset Types, Assets, Devices, Flows) is expressed as Resources with a Spec and a Status, applied in the style of Kubernetes, and Flownodes continuously reconciles actual state towards the Spec. We chose this over imperative CRUD so that an installation is reproducible from its Manifests and an Engineer can always ask "is reality what I declared?", which matters in mission-critical environments.

## Consequences

- The `flowctl` CLI, the Web API and the web UI are equal clients of one apply API. The server, not Git, is the source of truth; GitOps can be layered on top but is not enforced.
- Runtime state (Data Point values, Events, Flow executions, alerts) is observed, never applied.
- Discovered Devices may exist without anyone applying them, but attaching a Device to an Asset is always declared.
