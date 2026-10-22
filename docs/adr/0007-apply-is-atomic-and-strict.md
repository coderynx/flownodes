# Apply is atomic and strict

Applying a Manifest replaces each Resource's whole Spec, commits every Resource in it or none, and is rejected unless every reference between Resources resolves in the Namespace as it would be after the apply. Deletes follow the same rules: a batch delete is all-or-nothing, deleting a Resource that is still referenced is rejected, and nothing ever cascades or is pruned. We chose this over Kubernetes' per-object, merge-based, eventually-consistent apply because only the Registry writes Specs (ADR-0004), so field ownership buys nothing, and because a half-applied Manifest or a dangling reference is a declaration that can never be reconciled, which is exactly what an Engineer in a mission-critical environment must not discover at runtime.

## Consequences

- An Engineer sees every validation error at `apply` time, including errors in Resources the Manifest did not touch but whose meaning it changes, such as an Asset instance that now clashes with its changed Asset Type.
- Applies and deletes in one Namespace are serialised so that validation sees a consistent state. Namespaces never block each other.
- Removing a subtree, such as an Asset with its child Assets and Devices, needs one batch delete listing all of it; single deletes are blocked by the references between them.
- A field omitted from an applied Spec is removed. Clients that edit a Resource, such as the web UI, send the whole Spec and the Generation they edited, and get a conflict if it is stale.
