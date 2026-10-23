# Types are sealed and as private as they can be

A class or record is `sealed` unless production code derives from it, and every type and member carries the narrowest access modifier that still compiles: `private` for what one type uses, `internal` for what one project uses, `public` only for what another project calls. De-sealing and widening are edits made when a second caller appears, not defaults applied in advance. The two rules are one rule seen from two sides: sealing closes the inheritance surface, access closes the reference surface.

Both are decided by production code alone. A test project is not a caller: nothing is widened or de-sealed because a test needs to reach it. Tests get access through `InternalsVisibleTo` on the project under test, declared as an MSBuild item in its `.csproj`, never as an `[assembly: InternalsVisibleTo]` attribute in source, so the grant sits with the other build facts instead of reading as production code that knows about its tests:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="Registry.Application.Tests" />
</ItemGroup>
```

An implementation reached through an interface (a `DbContext` behind `IApplicationDbContext`, a hosted service, a pipeline behavior) is `internal`. What stays `public` in a module is what the next layer or the host calls: its `Bootstrap` classes, and the contracts an outer layer of the same module implements or consumes.

Generated code is left as its tooling writes it: EF migrations stay `public partial` because `dotnet ef` regenerates them that way, and `.editorconfig` marks `Database/Migrations/` as generated.

## Consequences

A `public` or unsealed type is always a statement about production: something outside the project calls it, or something derives from it. A project that can only be tested by making its internals `public` is telling us its contract is wrong. Where a framework demands an open type, the type is de-sealed at that point with the reason in the diff. An architecture test fails the build on an `InternalsVisibleTo` attribute; sealing and access are held by review.
