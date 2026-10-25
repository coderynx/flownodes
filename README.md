# Flownodes

Flownodes is an open-source automation platform that integrates IoT devices and external services. It is built around an **asset-centric** data model: you model the real-world things you care about (a train, a production line, a house) as Assets, attach the Devices that measure and control them, and automate them with Flows.

Think of it as Home Assistant designed for mission-critical, professional and industrial environments. It runs equally well on a home server for home automation.

## Who it is for

Flownodes is aimed first at technical users. They are comfortable with declarative configuration and a command line, and they want reproducible, version-controllable setups. Configuration works like Kubernetes: you `apply` Manifests with `flowctl`, through the Web API, or through the web UI, and Flownodes reconciles reality towards them.

The web UI is how Flownodes will later reach non-technical users. It writes the same Resources through the same API, so ease of use never needs a separate configuration model.

## Core ideas

- **Local-first**: no cloud service or internet connection is ever required.
- **Assets** form a tree. An Asset can be an ad-hoc one-off, such as your living room, or an instance of an **Asset Type**, so a fleet of 50 identical trains is configured once.
- **Devices** come from **Connections** to **Integrations**, which are plugins released independently of Flownodes. Devices attach to Assets.
- Assets expose their own **Data Points**, so Flows and dashboards keep working when hardware is replaced. Every value carries a **Quality** and a timestamp.
- **External Services** (weather APIs, ticketing, webhooks) are never Devices. They expose **Actions** and **Events**, and their data reaches Assets through **Flows**.
- **Flows** are graphs of **Nodes**. A Flow Execution survives a server failure and resumes where it stopped.
- **Alarms** follow an industrial active / acknowledged / cleared lifecycle.
- **Namespaces** isolate tenants within one installation. A single-person install just uses the `default` Namespace.
- It runs on a single Raspberry Pi or as a high-availability cluster, from the same codebase.

The full vocabulary is in [`GLOSSARY.md`](GLOSSARY.md), and the reasoning behind the big decisions is in [`docs/adr/`](docs/adr/).

## Out of scope

- Tracking equipment lifecycle or maintenance history. Flownodes is not an asset-management system.
- Long-term time-series storage. Use an Integration to an external database.
- Safety-critical control. Flownodes supervises and automates, but it does not replace PLC-level safety logic.

## Architecture at a glance

A .NET modular monolith, orchestrated locally with Aspire:

- **Registry** stores, validates and versions Resources (desired state) using plain database access on Postgres.
- **Engine** reconciles Resources into live state on Microsoft Orleans, talks to Devices and External Services, and runs Flows.
- **Core** holds shared application and presentation plumbing.

## License

Flownodes is licensed under the [GNU Affero General Public License v3.0](LICENSE). If you run a modified version as a network service, you must offer its source code to the service's users.
