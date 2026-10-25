# Tests share one naming, layout, assertion and mocking standard

Every test reads the same way, so a failing name says what broke and a reviewer finds Arrange, Act and Assert without decoding.

- **Name**: `Member_ShouldOutcome_WhenCondition`. `Member` is the method, endpoint or handler under test; where no single member is (API and architecture tests) it names the behaviour. `_WhenCondition` is dropped only when there is no condition. A `[Theory]` follows the same pattern, with `When` naming the varying dimension.
- **Layout**: `// Arrange`, `// Act` and `// Assert` comments. `// Arrange` is omitted when there is no setup; `// Act` and `// Assert` are always present. One Act per test; several assertions are fine when they describe one outcome.
- **Exceptions**: captured in Act (`var act = () => ...`), asserted in Assert (`act.ShouldThrow<T>()`, `await act.ShouldThrowAsync<T>()`).
- **Assertions**: Shouldly. `Assert.*` is not used, except `Assert.Fail` and the `Assert.Skip*` family, which Shouldly does not replace.
- **Kinds**: each module has `tests/<module>/<Module>.UnitTests` and `tests/<module>/<Module>.IntegrationTests`. Unit tests run the module's Domain and its CQRS handlers in memory: no host, no database, no Wolverine. Integration tests run the module in a real host against Postgres through Testcontainers, and prove what a unit test cannot: queries, constraints, HTTP mapping, authentication and messages. A rule is tested in detail where it lives, and once more through the endpoint that surfaces it.
- **Mocking**: NSubstitute, in unit tests only, for everything a handler depends on: the module's DbContext interface (its `DbSet`s built with MockQueryable's `BuildMockDbSet()`), contracts such as `IUserTokenRevoker`, `UserManager<User>` and `TimeProvider`. Domain entities and value objects are never mocked (ADR-0019); a test builds them through their factories. Integration tests mock nothing. Reuse the shared doubles before writing a new one: `TestDoubles` in Identity.UnitTests substitutes Identity's DbContext interface and `UserManager<User>`, and `TestDatabase` in Registry.UnitTests substitutes the Registry's `IApplicationDbContext`. `IApplicationDbContext` redeclares `SaveChangesAsync` with `new` over `IUserAccessSource.SaveChangesAsync`, so a test asserts a save through `((IUserAccessSource)database).Received()`. That holds until `SaveChangesAsync` is declared once.
- **Framework**: xUnit v3 on Microsoft.Testing.Platform (`global.json`). `tests/Directory.Build.props` references Shouldly for every project under `tests/`, and NSubstitute and MockQueryable.NSubstitute for `*.UnitTests` only.

Class and file names are not part of the standard.

## Consequences

An architecture test fails the build on a test whose name, markers or assertions break the standard; on a test project referencing Moq, FakeItEasy, FluentAssertions or NFluent; on a test project named or placed outside the two kinds; on a unit test project referencing an Infrastructure or Presentation project, TestHost, Testcontainers or Wolverine; and on any other test project referencing a mocking library. Unit tests' in-memory queries ignore Postgres collation and translation, which is why every query a handler runs is also exercised by an integration test. The exception pattern is held by review.
