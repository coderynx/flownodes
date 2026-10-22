# Flownodes

An open-source automation platform that integrates IoT devices and external services around an asset-centric data model, for industrial and professional environments as well as home automation.

## Language

### Asset model

**Asset**:
A real-world thing whose operation is being monitored or automated, such as a train. Assets form a tree: an Asset can have one parent Asset and many child Assets.
_Avoid_: Thing, equipment, object

**Asset Type**:
A reusable template for a kind of Asset, defining the child Assets, Data Points and Flows every Asset of that kind has. An Asset is either ad hoc or an instance of one Asset Type; an instance may extend its Asset Type, but never redefine what it declares.
_Avoid_: Template, class, model, profile

**Device**:
A physical or logical IoT unit, reached through a Connection, through which data is read and Actions are performed. A Device exists only once it is declared, as an instance of one Device Type; it is attached to at most one Asset, at any level of the tree.
_Avoid_: Sensor, node, endpoint

**Device Type**:
A kind of Device that an Integration defines, such as a Hue bulb or a Modbus register map. It determines how a Device of that kind is addressed and configured, and the Data Points, Actions and Events follow from that declaration. A Device Type is defined by an Integration and never declared.
_Avoid_: Device model, device profile, device class

**Data Point**:
A named, typed value with a current value, which can be read-only or writable, such as a temperature or a setpoint. Both Devices and Assets have Data Points.
_Avoid_: Signal, Point, Property, Channel, Tag, Entity

**Quality**:
The trustworthiness of a Data Point value: good, uncertain or bad. Every value carries a Quality and a source timestamp, and a value derived from a bad one is itself bad.
_Avoid_: Status, validity, health

**Asset Data Point**:
A Data Point that belongs to an Asset and describes the real-world thing independently of the hardware measuring it. Its value is mapped from a Device Data Point, computed from other Data Points, or written by a Flow. Writing to one mapped to a writable Device Data Point writes through to the Device.
_Avoid_: Virtual point, derived tag

**Action**:
A named operation that can be invoked, optionally with parameters, and that may return a result, such as opening a train's doors or fetching the current weather. An Action does something; it does not hold a value.
_Avoid_: Command, Method

**Event**:
Something that happened at a point in time, carrying a payload, which can trigger a Flow. An Event has no current value.
_Avoid_: Message, notification, signal

### Integrations

**Integration**:
A plugin that teaches Flownodes to talk to a protocol, a vendor's devices or an external service, such as MQTT, Modbus TCP, Philips Hue, Reolink cameras or a weather API. It defines one kind of Connection and any number of Device Types.
_Avoid_: Connector, driver, adapter, extension

**Connection**:
A configured instance of an Integration, e.g. a specific MQTT broker with its address and credentials. A Connection reaches Devices, and can expose Actions and Events of its own, such as pairing with a bridge or the forecasts of a weather API.
_Avoid_: Integration instance, account, link

**Discovery**:
A unit that a Connection can reach but that has not been declared as a Device. It is runtime state of the Connection, not a Resource.
_Avoid_: Discovered device, candidate, unclaimed device

**External Service**:
A non-device system reached through a Connection, such as a weather API or a ticketing system. Its Actions and Events belong to that Connection; it has no Data Points and is never modelled as a Device. Its data reaches Assets only through Flows.
_Avoid_: Virtual device, cloud device

### Automation

**Flow**:
An automation expressed as a graph of Nodes, triggered by Events or Data Point changes on Assets, Devices or External Services.
_Avoid_: Workflow, rule, automation, script

**Node**:
A single step in a Flow, such as a trigger, a condition, a transformation, a wait or an Action call. Never used for a server in the cluster.
_Avoid_: Block, step, task

**Flow Execution**:
One run of a Flow, from the trigger that started it to its completion, which may include long waits.
_Avoid_: Run, instance, job

**Alarm**:
An abnormal situation on an Asset that requires an Operator's attention, with a lifecycle of active, acknowledged and cleared. Raised by an Alarm Condition or by a Flow.
_Avoid_: Alert, notification, incident

**Alarm Condition**:
A declarative rule on an Asset Type's Data Points, such as a brake pressure staying below 3 bar for 10 seconds, that raises an Alarm while it holds.
_Avoid_: Alarm rule, trigger, threshold

### Configuration

**Namespace**:
The unit of tenant isolation within one installation. Every Connection, Secret, Asset Type, Asset, Device and Flow lives in exactly one Namespace and references only Resources in that Namespace. Every installation starts with a `default` Namespace.
_Avoid_: Tenant, organisation, workspace, project

**Resource**:
A declared piece of configuration with a Kind, a name, a Spec and a Status, such as a Connection, an Asset Type, an Asset, a Device or a Flow. A Resource is identified by its Kind and name within its Namespace. Runtime state such as Data Point values or Events is never a Resource.
_Avoid_: Object, entity, deployment, config item

**Kind**:
The type of a Resource, such as `Connection`, `AssetType`, `Asset`, `Device` or `Flow`.
_Avoid_: Resource type, class

**Manifest**:
A document containing one or more Resources, applied to Flownodes to request a change to desired state. A Manifest is applied as a whole or not at all.
_Avoid_: Deployment, config file, template

**Spec**:
The desired state of a Resource, as written by whoever applies it.
_Avoid_: Config, definition

**Status**:
The observed state of a Resource, as reported by Flownodes, such as whether a Connection is connected.
_Avoid_: State, health

**Generation**:
A counter on a Resource that increases every time its Spec changes. The Status records the Generation last reconciled, showing whether a change has been picked up.
_Avoid_: Version, revision number

**Revision**:
A recorded Spec of a Resource, with who applied it and when. The current Spec is the latest Revision.
_Avoid_: History entry, snapshot, version

**Secret**:
A Resource holding credentials, referenced by name from other Resources such as Connections. Its values can be written but never read back.
_Avoid_: Credential, password, key

**Reconciliation**:
The continuous process of driving actual state towards a Resource's Spec and reporting the outcome in its Status.
_Avoid_: Sync, deployment, provisioning

**Registry**:
The part of Flownodes that stores, validates and versions Resources: the source of truth for desired state.
_Avoid_: Catalog, repository, config store

**Engine**:
The part of Flownodes that reconciles Resources into running Connections, Devices, Assets and Flows, holds runtime state, and talks to Devices and External Services.
_Avoid_: Runtime, executor, worker

### Operation

**Silo**:
One running Flownodes server process. A Raspberry Pi deployment has one Silo; an HA deployment has several.
_Avoid_: Node, instance, server

**Operator**:
A role within a Namespace whose holder watches live state, acknowledges Alarms, and may invoke Actions, without changing configuration.
_Avoid_: User, viewer

**Engineer**:
A role within a Namespace whose holder configures its Connections, Secrets, Asset Types, Assets, Device attachments and Flows.
_Avoid_: Developer, user

**Administrator**:
An installation-wide role whose holder creates Namespaces and grants Operator and Engineer roles in them.
_Avoid_: Admin, superuser, root
