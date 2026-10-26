# Services are injected; static classes stay pure

Code that needs a service gets it from the container: it is a `sealed` class with the service in its constructor, registered in its module's `Bootstrap`. A static class only holds code that works on values. We decided this after the Registry and Identity built access checks, token revocation, secret storage and revision history as static helpers. Each took the module's DbContext, and sometimes a vault, a revoker, a clock or a configuration value, as a method parameter, so every caller had to pass them in. Their handler tests had to fill DbSets just to get past an access check. Meanwhile the Engine's `IRoleGrants` is injected, and its tests stub one call.

## The rule

A static class may not:

- **take a service as a parameter**, including the `this` parameter of an extension method. A service is an interface declared in the solution (a DbContext interface such as `IApplicationDbContext`, `ISecretVault`, `IIntegrationSchemas`), `TimeProvider`, `UserManager<>`, `DbContext`, `IServiceProvider`, `ISender` or `IGrainFactory`;
- **read the clock or a random source on its own**: `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`, `TimeProvider.System`, `RandomNumberGenerator`, `Random.Shared`, `Guid.NewGuid` or `Guid.CreateVersion7`. A value a test has to assert on comes from an injected `TimeProvider` or a service.

A static class that keeps to both limits is fine and stays static: `ToDto()` mappings (ADR-0019), error and message factories, constants, validators and diffs over `JsonElement`, value checks, and extension methods on framework types such as `PropertyBuilder` or `Error`. Giving pure code an interface only adds a stub to every test that touches it.

## Exempt

- **The composition root**: the classes under a project's `Bootstrap/` folder. Registering services is their job, `TimeProvider.System` included.
- **Handlers that the framework calls with parameters it resolves per call**: Wolverine message handlers (`*MessageHandler` under `Messaging/`) and minimal-API route handlers in a class that calls `Map{Get,Post,Put,Delete,Patch}`. Their parameters are injected, just per method instead of per constructor. ADR-0013's pre-generated handler code makes static Wolverine handlers the cheaper form.

Any other exemption is a decision recorded here.

## Considered options

- **Review alone.** We rejected it because the static helpers spread through review in the first place. Each one looked like a small, sensible shortcut.
- **A list of exempt classes in the test.** We rejected it because the four helpers it would have listed (`UserAccessProjection`, `PasswordPolicy`, `NamespaceCreation`, `AssetSources`) were easy to fix. A list like that grows faster than it shrinks.

## Consequences

An architecture test reads the production sources under `apps/` and `packages/` as text and fails on a static class that takes a service parameter or reads the clock or a random source. Two cases are left to review: a configuration value such as a retention count passed in as a plain parameter, and a static method on a non-static type. Handler unit tests stub the new services (ADR-0025), and each service is tested on its own.
