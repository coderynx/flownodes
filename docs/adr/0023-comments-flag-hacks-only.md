# A code comment flags a hack, and nothing else

Names, types and structure say what code does, XML docs say what a signature promises (ADR-0021), and ADRs say why the design is shaped as it is. A comment is left for the one thing none of those carry: a place where the code looks wrong, or behaves unexpectedly, and must stay that way.

- **Allowed**: a comment that flags a workaround, a framework or library quirk, or a deliberate deviation a reader would otherwise "fix". It says why this stays, not what the code does. It opens with `// HACK:`; continuation lines are plain `//`, and a trailing comment carries the marker too.
- **Not allowed**: narration, restated names, section labels, commented-out code and ordinary design rationale. Rationale about a contract goes in its XML doc; rationale about the design goes in an ADR.
- **Exempt**: `///` XML docs, the `// Arrange`, `// Act` and `// Assert` comments of a test (ADR-0025), and generated migrations.
- **Other languages**: the rule applies to all code in the repo, SQL and config included. It is enforced for C# and held by review elsewhere.

## Consequences

An architecture test scans every `.cs` file under `apps/`, `packages/` and `tests/` and fails on any comment that is neither marked, a continuation of a marked block, nor exempt. Whether a `// HACK:` comment really flags a hack is held by review. The marker gives the codebase one greppable list of its own workarounds.
