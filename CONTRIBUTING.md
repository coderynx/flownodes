# Contributing to Flownodes

Flownodes is pre-release: nothing is deployed yet, and the API, Resource shapes, Integration SDK and database schema still change whenever that improves the design. Expect breaking changes between commits.

## Before you start

- Read [`README.md`](README.md) for what Flownodes is and who it is for.
- Learn the vocabulary in [`GLOSSARY.md`](GLOSSARY.md) and use its terms in issues, code and commit messages.
- Read the ADRs in [`docs/adr/`](docs/adr/) that touch the area you want to change.
- For anything larger than a small fix, open an issue first so the design can be agreed before you write code.

## Development setup

You need:

- The .NET SDK version pinned in [`global.json`](global.json).
- Docker, for Postgres in local runs and integration tests.

Run Flownodes locally through the Aspire AppHost:

```sh
dotnet run --project apps/AppHost
```

The API always listens on `https://localhost:7076` and `http://localhost:5164`, the ports in its `https` launch profile, so a `flowctl` sign-in keeps working across restarts.

Install `flowctl` from your checkout as a global .NET tool, and run the script again to pick up later changes:

```sh
scripts/install-flowctl.sh
flowctl login --server https://localhost:7076
```

The .NET SDK installs tools into `~/.dotnet/tools`; add it to your `PATH` if `flowctl` is not found.

The first build sets `core.hooksPath` to `.githooks`: a pre-commit hook formats your staged C# files, and a commit-msg hook rejects messages that link an agent session.

## Coding standards

The standards are listed in [`AGENTS.md`](AGENTS.md), and each one points to the ADR behind it. The build and the architecture tests enforce most of them: analyzer warnings fail the build, and `#pragma` and `[SuppressMessage]` are not allowed. After changing C#, run `scripts/format.sh` instead of laying out code by hand.

## Before you open a pull request

```sh
dotnet build Flownodes.slnx
dotnet test --solution Flownodes.slnx
```

Both must pass, and CI runs them again on every pull request. Keep each pull request focused on one change, link the issue it resolves, and explain what changed and why.

## License

Flownodes is licensed under the [GNU Affero General Public License v3.0](LICENSE). By contributing, you agree that your contribution is licensed under the same terms.
