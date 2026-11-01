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
- Node.js 24 or later, to build the web UI. `-p:SkipWebClient=true` skips that step for a backend-only build.

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

The first build sets `core.hooksPath` to `.githooks`: a pre-commit hook rejects staged changes that hold a secret and formats your staged C# files, and a commit-msg hook rejects messages that link an agent session or that are not Conventional Commits.

The secret scan runs [gitleaks](https://github.com/gitleaks/gitleaks) at the version pinned in `scripts/gitleaks.sh`, which CI also uses to scan the whole history. It uses a local `gitleaks` of that version when one is installed and the pinned Docker image otherwise; with neither, it prints a message and skips the scan. To scan without Docker, install gitleaks with `brew install gitleaks` on macOS, or download the release for your platform from [the gitleaks releases](https://github.com/gitleaks/gitleaks/releases) and put it on your `PATH`.

## Coding standards

The standards are listed in [`AGENTS.md`](AGENTS.md), and each one points to the ADR behind it. The build and the architecture tests enforce most of them: analyzer warnings fail the build, and `#pragma` and `[SuppressMessage]` are not allowed. After changing C#, run `scripts/format.sh` instead of laying out code by hand.

## Commit messages

A commit subject is a [Conventional Commit](https://www.conventionalcommits.org): `<type>[(scope)][!]: <description>`, for example `feat(engine): enforce Namespace roles`. The `commit-msg` hook checks it, and `scripts/check-commit-subject.sh` lists the allowed types and scopes. Squash is the only merge mode, so the pull request title must follow the same format; a CI check verifies it. Before the first public release, breaking changes need neither `!` nor a `BREAKING CHANGE:` footer, though both are valid. `git revert` writes `Revert "…"`, which the hook lets through; type `revert: …` yourself for the pull request title. [ADR-0027](docs/adr/0027-conventional-commits.md) has the reasoning.

## Before you open a pull request

```sh
dotnet build Flownodes.slnx
dotnet test --solution Flownodes.slnx
```

Both must pass, and CI runs them again on every pull request that changes more than Markdown. Keep each pull request focused on one change, link the issue it resolves, and explain what changed and why.

## License

Flownodes is licensed under the [GNU Affero General Public License v3.0](LICENSE). By contributing, you agree that your contribution is licensed under the same terms.
