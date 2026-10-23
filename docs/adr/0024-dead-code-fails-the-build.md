# Dead code and unused usings fail the build

Code nobody calls and usings nothing needs are noise for the next reader, and an agent will happily extend or "fix" a member that is dead.

- **No unused usings.** Every `using` is needed by the file that carries it.
- **No unused private members, parameters or locals.** Sonar covers these (S1144, S1172, S1481); IDE0051, IDE0052 and IDE0060 close its gaps.
- **No unused types across projects.** A `public` or `internal` type nothing in the solution references, and no framework invokes, is deleted.
- **Core is not exempt.** A shared contract with no implementation and no caller is dead, however likely its first use looks; it arrives with that use.

## Enforcement

IDE0005, IDE0051, IDE0052 and IDE0060 are warnings in `.editorconfig`, which the build turns into errors. IDE0005 only reports when `GenerateDocumentationFile` is on, so `Directory.Build.props` sets it for every project; tests keep CS1591 off.

Roslyn analyzers see one project at a time, so an architecture test reads the production sources under `apps/` and `packages/` as text, comments stripped, and fails on any type whose name appears in no other file and nowhere else in its own file. Types a framework invokes without a visible reference are exempt by rule:

- EF `IEntityTypeConfiguration<>` implementations, found by `ApplyConfigurationsFromAssembly`;
- MediatorKit request handlers (`IRequestHandler`, `ICommandHandler`, `IQueryHandler`), found by assembly scanning;
- `IEndpoint` implementations, found by `MapEndpoints`;
- classes holding a `Main` entry point;
- static classes holding extension methods.

Any other exemption is a decision recorded here. The test sees types only; unused public or internal methods and properties are held by review.

We rejected running a third-party tool outside the build, because it reports too late for an agent iterating locally (ADR-0016), and review alone, because nothing else catches dead code across projects and it is what accumulates.
