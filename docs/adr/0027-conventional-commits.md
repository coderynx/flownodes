# Commit messages follow Conventional Commits, and pull requests are squash-merged

Every commit on `main` is one pull request, and its subject is a Conventional Commit: `<type>[(scope)][!]: <description>`. The history reads as a list of changes by kind, and a changelog and version tool can be added at the first public release without rewriting anything.

- **Types**: `feat`, `fix`, `docs`, `refactor`, `perf`, `test`, `build`, `ci`, `chore` and `revert`. There is no `style`: `scripts/format.sh` owns layout (ADR-0022).
- **Scopes**: optional, from a fixed list: `core`, `engine`, `identity`, `registry`, `sdk`, `flowctl`, `host`, `integrations` and `deps`. They follow the directories under `packages/`, `apps/` and `integrations/`.
- **Subject**: at most 72 characters, no trailing period. The description is an imperative sentence, and Glossary terms keep their capitals.
- **Breaking changes**: `!` is valid but not required before the first public release, because the pre-release rule is to change the API, the Manifests, the SDK and the schema without ceremony. From the first public release on, a breaking change to any of them carries `!`.
- **Merging**: squash only. The pull request title is the subject and the pull request body is the commit body, so merge commits and rebase merges are off. Commits on a branch need not be tidy.
- **History**: commits before this ADR are not rewritten.

## Consequences

`scripts/check-commit-subject.sh` holds the types, the scopes and the checks. The `commit-msg` hook runs it on every commit, except the `Merge`, `fixup!`, `squash!` and `amend!` subjects Git writes itself. The `PR title` workflow runs it on every pull request title with no exemptions, since the title becomes the commit on `main`. `main` is not protected, so a bad title shows a red check and does not block the merge.

Release automation is not part of this decision. A tool that reads the history, whether commitlint, release-please or another, needs its own ADR when releases start; the type and scope lists are the contract it would read.
