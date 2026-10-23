# Tests share one naming, layout, assertion and mocking standard

Every test reads the same way, so a failing name says what broke and a reviewer finds Arrange, Act and Assert without decoding.

- **Name**: `Member_ShouldOutcome_WhenCondition`. `Member` is the method, endpoint or handler under test; where no single member is (API and architecture tests) it names the behaviour. `_WhenCondition` is dropped only when there is no condition. A `[Theory]` follows the same pattern, with `When` naming the varying dimension.
- **Layout**: `// Arrange`, `// Act` and `// Assert` comments. `// Arrange` is omitted when there is no setup; `// Act` and `// Assert` are always present. One Act per test; several assertions are fine when they describe one outcome.
- **Exceptions**: captured in Act (`var act = () => ...`), asserted in Assert (`act.ShouldThrow<T>()`, `await act.ShouldThrowAsync<T>()`).
- **Assertions**: Shouldly. `Assert.*` is not used, except `Assert.Fail` and the `Assert.Skip*` family, which Shouldly does not replace.
- **Mocking**: NSubstitute, only for contracts a module is reached through: Core's abstractions, a layer's interfaces such as `IApplicationDbContext`, and the message contracts of ADR-0012. Domain entities and value objects are never mocked (ADR-0019), and neither is the database: tests that need one use Testcontainers.
- **Framework**: xUnit v3 on Microsoft.Testing.Platform (`global.json`). Shouldly is referenced for every project under `tests/` through `tests/Directory.Build.props`; NSubstitute only by the projects that use it.

Class and file names are not part of the standard.

## Consequences

An architecture test fails the build on a test whose name, markers or assertions break the standard, and on a test project referencing Moq, FakeItEasy, FluentAssertions or NFluent. What gets mocked and the exception pattern are held by review.
