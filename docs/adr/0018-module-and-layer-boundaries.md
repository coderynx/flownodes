# Modules reach only their own inner layers and Core

Each module under `packages/<module>/` is split into `<Module>.Domain`, `<Module>.Application`, `<Module>.Infrastructure` and `<Module>.Presentation`, and dependencies point inward: Application references Domain; Infrastructure and Presentation reference Application. A module project may also reference Core's project of the same layer, and nothing else: never another module's project. Core references only its own inner layers. This is how ADR-0012 shows up in the project graph: a module that cannot compile against another module cannot call it.

Core holds only what more than one module needs. A type one module uses alone stays in that module until the second caller exists, because everything in Core is something every module compiles against. The message contracts ADR-0012 describes are the exception that proves it: they will live in their own shared package when the first message exists.

The host (`apps/Flownodes`) composes the monolith through each module's `Bootstrap` namespace and touches nothing else in a module. Anything the host needs from a module is a method on its `Bootstrap` classes.

A test project named for one module (`<Module>.*.Tests`) references only that module's projects and Core. Tests that cross modules belong in a project of their own.

## Consequences

Architecture tests fail the build on a module project that is misnamed or references outside its inner layers and Core's counterpart, on host code that reaches past a module's `Bootstrap`, and on a module's test project that references another module. Whether something belongs in Core is held by review.
