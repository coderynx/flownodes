## Project context

Product scope and audience: `README.md`. Read it, and the out-of-scope list below, before planning a feature, triaging an issue or proposing new scope.

Out of scope:

- Tracking equipment lifecycle or maintenance history. Flownodes is not an asset-management system.
- Long-term time-series storage. Use an Integration to an external database.
- Safety-critical control. Flownodes supervises and automates, but it does not replace PLC-level safety logic.

**Pre-release, until further notice**: nothing is deployed and nobody depends on Flownodes yet. Change the API, the Resource and Manifest shapes, the Integration SDK, the database schema and stored data whenever it improves the design. Make the change directly, and leave out migrations, deprecation paths and compatibility shims. The SDK versioning promise in ADR-0003 starts at the first public release.

## Coding standards

Each rule below is an ADR; read the ADR before changing code in its area. The build and the architecture tests (`tests/Architecture.Tests`) enforce what a tool can prove, and review holds the rest.

- **Build**: analyzer warnings fail the build. Fix the code. Turn off a rule that fights a deliberate decision in `.editorconfig` with its reason; `#pragma` and `[SuppressMessage]` fail an architecture test. Package versions live in `Directory.Packages.props`. ADR-0016.
- **Access**: types are `sealed` and as private as production callers allow. Tests reach internals through an `<InternalsVisibleTo>` item in the `.csproj`. ADR-0017.
- **Boundaries**: a module project references its own inner layers (Application → Domain; Infrastructure, Presentation → Application) and Core's matching layer, never another module. The host reaches a module only through its `Bootstrap`. ADR-0018.
- **Domain**: rich entities (private ctor, `private set`, a `Create` factory, behaviour in methods) and value objects for rule-bearing values. Responses are DTOs mapped with `ToDto()`. ADR-0019.
- **HTTP**: a use case is `<Feature>/<UseCase>/Command.cs` or `Query.cs` in Application, mirrored by `<Feature>/<UseCase>/Endpoint.cs` in Presentation. Routes sit under `/api/v1`, applied by Core; every endpoint has `.WithName(...)`. ADR-0020.
- **Docs**: only types and members another project can access carry XML docs, with every tag and in `GLOSSARY.md` terms; internal code carries none. ADR-0021.
- **Layout**: run `scripts/format.sh` after changing C#; never lay code out by hand. ADR-0022.
- **Comments**: a comment flags a hack and opens with `// HACK:`. ADR-0023.
- **Dead code**: delete what nothing references, Core contracts included. ADR-0024.
- **Tests**: unit tests substitute every handler dependency; integration tests use a real host and Postgres. ADR-0025.

Before each commit, build the solution and run the unit and architecture tests:

```sh
dotnet build Flownodes.slnx
for project in tests/Architecture.Tests tests/*/*.UnitTests; do dotnet test --project "$project" --no-build; done
```

Also run a module's integration tests when the commit changes its Infrastructure, Presentation, the queries a handler runs or its messages. Unit tests run queries in memory and miss Postgres collation and translation:

```sh
dotnet test --project tests/<module>/<Module>.IntegrationTests
```

CI runs the whole suite.

## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
