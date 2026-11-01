# Flownodes

[![CI](https://github.com/coderynx/flownodes/actions/workflows/ci.yml/badge.svg)](https://github.com/coderynx/flownodes/actions/workflows/ci.yml)
[![License: AGPL v3](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](LICENSE)

Flownodes is an open-source automation platform for IoT devices and external services. You describe the real-world things you care about, such as a train, a production line or a house, as Assets. Then you attach the Devices that measure and control them and automate them with Flows.

If you know Home Assistant, Flownodes covers similar ground but is built for mission-critical professional and industrial sites. It also runs fine on a home server.

> [!WARNING]
> Flownodes is pre-release and has no published version yet. The API, the Manifest format, the Integration SDK and the database schema still change between commits, without migrations. Don't run it for anything you depend on.

## Who it's for

Right now, technical users: people who are happy with a command line and declarative configuration, and who want setups they can reproduce and keep in version control.

Configuration works the way it does in Kubernetes. You `apply` Manifests with the `flowctl` CLI, the Web API or the web UI, and Flownodes keeps reconciling the running system toward what they describe. The web UI is meant to open Flownodes up to non-technical users later. It writes the same Resources through the same API, so there is only one configuration model to learn.

## How it works

Assets form a tree. An Asset can be a one-off, like your living room, or an instance of an Asset Type, so a fleet of 50 identical trains is configured once.

Devices come from Connections to Integrations. Integrations are plugins with their own release cycle, separate from Flownodes itself. Once a Device exists, you attach it to an Asset. Each Asset exposes its own Data Points, and Flows and the web UI read those rather than the Device directly, so replacing a sensor doesn't break them. Every value carries a Quality and a timestamp.

External Services, such as a weather API, a ticketing system or a webhook, are never modelled as Devices. They expose Actions and Events, and Flows carry their data to Assets.

A Flow is a graph of Nodes. If the server fails partway through a Flow Execution, the execution resumes where it stopped.

A few other properties:

- It is local-first. Nothing requires a cloud service or an internet connection.
- Alarms follow the industrial lifecycle of active, acknowledged and cleared.
- Namespaces keep tenants apart inside one installation. A single-person setup just uses the `default` Namespace.
- The same codebase runs on a single Raspberry Pi or as a high-availability cluster.

## Running it locally

You need the .NET SDK version pinned in [`global.json`](global.json) and Docker, which runs Postgres for local runs and integration tests.

```sh
git clone https://github.com/coderynx/flownodes.git
cd flownodes
dotnet run --project apps/AppHost
```

This starts the whole system through the .NET Aspire AppHost, with the web UI on the host's origin. To build and run the tests:

```sh
dotnet build Flownodes.slnx
dotnet test --solution Flownodes.slnx
```

Building needs Node.js 24 or later, for the web UI. A backend-only build skips it with `-p:SkipWebClient=true`. An Installation never needs Node.js.

## Architecture

Flownodes is a .NET modular monolith, orchestrated locally with Aspire. It has four modules:

- Registry stores, validates and versions Resources, the desired state, in Postgres.
- Engine runs on Microsoft Orleans. It reconciles Resources into live state, talks to Devices and External Services, and runs Flows.
- Identity issues tokens and handles sign-in.
- Core holds the plumbing the other modules share.

The `Web` package serves the web UI, a React single-page app, from the host's origin.

## Contributing

Contributions are welcome. Start with [`CONTRIBUTING.md`](CONTRIBUTING.md), which covers the development setup, the coding standards and what a pull request needs. For anything bigger than a small fix, open an issue first so the design can be agreed before you write code.

Questions and bug reports go in [GitHub Issues](https://github.com/coderynx/flownodes/issues). Report security vulnerabilities privately, as described in [`SECURITY.md`](SECURITY.md).

## License

Flownodes is licensed under the [GNU Affero General Public License v3.0](LICENSE). If you run a modified version as a network service, you must offer its source code to the people who use that service.
