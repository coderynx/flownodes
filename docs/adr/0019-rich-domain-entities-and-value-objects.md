# Domain entities are rich, and rule-bearing values are value objects

Entities in `<Module>.Domain` own their invariants. A handler loads an aggregate, calls a method on it, and saves; the rule lives in one place and is tested without HTTP or a database. We chose this over anemic entities mutated by handlers, where the same rule (a version bump, a state transition, a length limit) is re-implemented in every handler that touches it.

## Shape

- An entity has a private parameterless constructor, which EF Core materialises through. Identity fields are `required { get; init; }`; state that changes is `{ get; private set; }`. A static `Create(...)` factory is the only way to build one.
- Collections are a private `List<T>` field exposed as `IReadOnlyList<T>`. Children are changed only through their aggregate root.
- Methods that stamp a time take `DateTimeOffset now` as a parameter, so they stay pure.
- There is no `Entity` base class until domain events give it something to hold beyond an id.

## Failure

- A value object built from untrusted input exposes `static bool TryCreate(raw, out T value)`.
- `Create` throws `ArgumentException`. It is used on data that is already trusted, such as database rows and other modules' messages.
- An illegal transition on an entity throws `InvalidOperationException`. No request path should reach one, so reaching one is a bug.
- Handlers return `Coderynx.Functional` `Result` or `Option` for expected failure or absence (ADR-0020).

## Value objects

A value is wrapped only when it carries a rule or when mixing it up would be a bug. A string with no rule stays a string.

- **Typed ids** are `readonly record struct`s and stay inside their module. Messages between modules carry `Guid`, and the module converts at its seam.
- **Values with rules** are `sealed record`s built through a validating factory, so an invalid instance cannot exist.
- **Persistence.** A single-value value object maps through `HasConversion`, which keeps it a real, indexable column. A multi-field one maps through `ComplexProperty`. Database check constraints that mirror a value object's rule are welcome.

## Where the database owns the transition

Some state changes are raw SQL because atomicity is the point. These are not moved into load-modify-save; the *decision* becomes a pure domain function, and the SQL applies what it returns.

## The API does not serialise the domain

Responses are DTOs in the module's Presentation. Shared ones live in `<Module>.Presentation/<Feature>/Contracts/`, next to an `internal static class <Entity>Mappings` holding `ToDto()` extension methods. There is no `ToEntity()`, because creation must go through the factory. Domain types carry no JSON attributes, and enums serialise as strings.

## Consequences

The whole ADR is held by review; `.editorconfig` switches off Sonar's S1144 under `*.Domain/` so EF's private setters do not read as dead code (ADR-0016).
