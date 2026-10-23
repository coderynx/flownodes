# Analyzers run in the build and a warning fails it

Code quality is enforced where agents and humans already look: `dotnet build`. `Directory.Build.props` turns on `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` for every project and references `SonarAnalyzer.CSharp` from all of them, tests included, with Sonar's default "Sonar way" rules. A violation is a build failure, so an agent sees it in the same loop as a compile error. We chose this over a SonarQube server, which reports after the build, too late for an agent iterating locally, and over the built-in .NET analyzers' `AnalysisMode`, because Sonar's ruleset is curated for findings a reviewer would raise rather than style.

Every package version lives in `Directory.Packages.props` (central package management, with transitive pinning), so one file answers which version of anything the solution runs.

## Turning a rule off

A rule that fights a deliberate decision is switched off in `.editorconfig`, with a comment naming the ADR or the reason. `#pragma warning disable` and `[SuppressMessage]` are not used, so every exception to the rules sits in one file a reviewer can read. An architecture test fails the build on an inline suppression.

The rules off at adoption:

- S3871 (exceptions must be public), everywhere: exceptions that never leave their module stay internal (ADR-0017).
- S1144 (unused private member), under `*.Domain/` only: EF sets an entity's private setters through reflection, which Sonar cannot see (ADR-0019).

## Consequences

Upgrading an analyzer can bring new rules that break the build. That is the point: a bump is a change like any other, fixed or turned off in `.editorconfig` in the same commit.
