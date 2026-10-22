# A Device's surface is derived from its Spec

Every Device is an instance of a Device Type defined by its Integration, and its Data Points, Actions and Events are a pure function of its Spec and that Device Type. The Registry evaluates this function at apply time, so a reference to a Device Data Point, such as an Asset Data Point mapping, is validated like any other reference (ADR-0007). We chose this over letting a Device report its surface once it connects because a mapping to a Data Point that only exists at runtime is a dangling reference an Engineer would discover too late, and because it keeps everything usable declared (ADR-0008). Where the surface comes from the Engineer, such as MQTT topics or Modbus registers, it is written in the Device's config; where the Integration knows it, such as a Hue bulb, the Device Type fixes it.

## Consequences

- Anything a device reveals only at runtime, such as an OPC UA browse result or a camera's capabilities, must be declared in the Device's Spec before Flows or Assets can use it. A Discovery can pre-fill that Spec.
- Integration code (the surface function and an optional config validator) runs in the Registry's apply path. It must be synchronous, deterministic and free of I/O.
- Upgrading an Integration can change or remove a Device Type's surface. Stored Specs stay unchanged; the Engine reports `DeviceTypeMissing` or `DataPointMissing` in the affected Resources' Status, and the next apply is validated against the new surface.
- During a rolling upgrade of an HA cluster, Silos briefly run different Integration versions, so apply-time validation depends on which Silo serves the request. This is accepted for v1.
