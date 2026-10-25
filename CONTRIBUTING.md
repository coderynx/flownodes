# Contributing to Flownodes

Flownodes is pre-release: nothing is deployed yet, and the API, Resource shapes, Integration SDK and database schema still change whenever that improves the design. Expect breaking changes between commits.

## Before you start

- Read [`README.md`](README.md) for the product scope and what is out of scope.
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

The first build sets `core.hooksPath` to `.githooks`, so a pre-commit hook formats your staged C# files.

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
