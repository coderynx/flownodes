# Schemas the Web API exposes for a Manifest editor

Research for issue #103, under the map #98 "Web UI ready". It reports what exists and what is missing. It does not design a fix.

**Question.** A Manifest editor has to validate and autocomplete each Kind's Spec, including each Integration's config schema (ADR-0009), and it has to send back the Generation it edited (ADR-0007). What does the Web API expose for this today: OpenAPI component schemas, a list of Kinds, Integration config schemas, the Generation on reads? What is missing?

**Sources.** This repo's code at `origin/main` 78229e3. Citations are `path:line`. To check the generated document, I dumped `/openapi/v1.json` from a Development-mode Registry host built the way `tests/registry/Registry.IntegrationTests/RegistryApp.cs` builds one, using a throwaway test that I did not commit. That dump covers only the Registry's endpoints. What I say about Engine and Identity schemas comes from reading their code, and I label it as inference.

## Summary

| Need | Today |
| --- | --- |
| OpenAPI document | Yes, in Development only, generated at runtime by `Microsoft.AspNetCore.OpenApi`. |
| Typed Spec per Kind | No. `spec` is `JsonElement`, which becomes the empty schema `{}`. The Manifest request body is `{}` too. |
| Correct response schemas | No. Every nested `Endpoint.Response` collapses into one component, so `ApplyManifest`, `ListResources` and `ListNamespaces` claim to return a list of Revisions. |
| List of Kinds | No endpoint. The Kinds are fixed in DI, and `{kinds}` is a free string. |
| Integration config schemas | They are declared in the SDK (`ConfigSchema`) but never exposed. `GET /engine/integrations` lists Integrations by id, version, status and Device Type names only. |
| Generation on reads | Yes, as `metadata.generation` on Get and List. |
| Stale Generation | Reported as `409 resource.generation_conflict`. Expected and actual Generation appear only in the `detail` text, and only for the first stale Resource. |
| Server-side validation | Yes. `POST /registry/apply?dryRun=true` returns every problem with a path. The OpenAPI document gives that 422 body the wrong shape. |

## 1. How the OpenAPI document is generated

- `AddPresentation`, which every module calls once per host, registers `AddOpenApi`. It names a nested schema `"{DeclaringType.Name}.{Type.Name}"` (`packages/core/Core.Presentation/Bootstrap/DependencyInjection.cs:42-45`).
- `UsePresentation` maps `/openapi/v1.json` and Scalar at `/scalar`, both anonymous and both **only when `IsDevelopment()`** (`packages/core/Core.Presentation/Bootstrap/HostConfiguration.cs:40-45`). A production Installation serves no OpenAPI document. Nothing generates the document at build time: the repo has no `Microsoft.Extensions.ApiDescription.Server`, and the only OpenAPI-related setting in `apps/Flownodes/Flownodes.csproj:24` is the interceptor namespace. The Web UI therefore has no committed document to generate a client from.
- Endpoints describe themselves with `.Produces<T>()`, `.Accepts<T>()` and `.WithName()`. ADR-0020 requires this (`docs/adr/0020-http-use-cases-as-mirrored-slices.md:14`).
- Integers come out as `"type": ["integer", "string"]` with a numeric `pattern`. One example is `metadata.generation`. This is the .NET 10 generator's default output for numbers it can also read from strings (observed in the dump). A TypeScript generator will type these as `number | string`.

### Component schemas in the dump (Registry only)

`DeleteResponse`, `DeletedDto`, `Endpoint.Response`, `HttpValidationProblemDetails`, `JsonElement`, `JsonObject`, `ProblemDetails`, `ResourceDto`, `ResourceMetadataDto`, `RevisionAuthorDto`, `RevisionDto`.

- **Specs are opaque.** `ResourceDto.Spec` is a `JsonElement` and `Status` is a `JsonObject?` (`packages/registry/Registry.Presentation/Resources/Contracts/ResourceDto.cs:8-13`). In the dump, `JsonElement` is `{}` (any value) and `JsonObject` is `{"type":"object"}`. No Kind's Spec has a schema.
- **The Manifest body is opaque.** `ApplyManifest` declares `.Accepts<JsonElement>("application/json", "application/yaml")` (`packages/registry/Registry.Presentation/Resources/ApplyManifest/Endpoint.cs:44`), so its request body schema is `{}` for both media types. The document does not even describe the envelope (`apiVersion`, `kind`, `metadata`, `spec`).
- **`Endpoint.Response` collision.** Every endpoint class is named `Endpoint` (ADR-0020), so the naming rule at `DependencyInjection.cs:43-44` gives every nested `Response` the same id, `Endpoint.Response`. In the dump that one component is `{items: RevisionDto[], total}`, and **five operations reference it**: `ListRevisions`, `ListNamespaceRevisions`, `ListResources`, `ListNamespaces` and `ApplyManifest`. The OpenAPI document therefore misdescribes `ListResources` (really `{items: ResourceDto[], total}`, `packages/registry/Registry.Presentation/Resources/ListResources/Endpoint.cs:59`) and `ApplyManifest` (really `{dryRun, resources: AppliedDto[]}`, `ApplyManifest/Endpoint.cs:73-83`). `AppliedDto` and `ChangeDto` appear nowhere in the document. ADR-0020 says "two endpoints' `Response` records stay two schemas", but the generated document does not do that. Across the repo, 16 `Endpoint.cs` files declare a nested `record Response(`. Inference: when the full host runs, the Engine's and Identity's nested `Response` records collide under the same id as well.
- **Route parameters are free strings.** `{kinds}` in `ListResources`, `GetResource` and the other routes is `{"type":"string"}`, with no enum (dump; `GetResource/Endpoint.cs:40`).

## 2. Is there an endpoint listing Kinds?

No. The routes are `/registry/apply`, `/registry/delete`, `/registry/namespaces[/{name}[/revisions]]` and `/registry/namespaces/{namespace}/{kinds}[/{name}[/revisions]]`. None of them lists Kinds (dump; `packages/registry/Registry.Presentation/**/Endpoint.cs`).

- The Kinds are compiled in: `SecretKind`, `ConnectionKind`, `DeviceKind`, `AssetTypeKind`, `AssetKind` and `FlowKind` are registered as `IResourceKind` singletons, and a `KindCatalog` is built from them (`packages/registry/Registry.Application/Bootstrap/DependencyInjection.cs:29-38`). `Namespace` is handled apart from the catalogue (`packages/registry/Registry.Domain/Resources/KindCatalog.cs:6`; `packages/registry/Registry.Application/Resources/KindRoutes.cs:8`).
- `IResourceKind` exposes only `Name`, `Plural`, `Validate(spec, namespace)` and `References(spec)` (`packages/registry/Registry.Domain/Resources/IResourceKind.cs:12-38`). It carries no declarative schema to publish.
- An unknown Kind is found only after the fact: an apply gets a problem at path `kind`, "There is no Kind called 'X'." (`packages/registry/Registry.Application/Resources/ManifestReader.cs:160-163`), and a route gets `404 kind.not_found` (`packages/registry/Registry.Application/Resources/Errors.cs:13-14`).

## 3. How each Kind's Spec is validated, and why no schema can be derived from it

Each Kind validates with hand-written C# that walks a `JsonElement`. No schema object exists to serialise.

- `ConnectionKind` accepts only `integration` and `config`, requires `integration` to be an installed id, and passes `config` to the Integration's validator (`packages/registry/Registry.Application/Resources/ConnectionKind.cs:17-45`).
- `DeviceKind` accepts `connection`, `type`, `address`, `config` and `asset`. It resolves the Connection's Integration and the Device Type, then calls that type's address and config validators (`packages/registry/Registry.Application/Resources/DeviceKind.cs:18-87`).
- `AssetTypeKind` accepts `dataPoints` and `alarms` (`packages/registry/Registry.Application/Resources/AssetTypeKind.cs:12-33`). `FlowKind` (625 lines) holds its node types, fields, edges, operators and limits in private static arrays and dictionaries (`packages/registry/Registry.Application/Resources/FlowKind.cs:11-36`).
- The Manifest envelope is fixed in code: top-level fields `apiVersion`, `kind`, `metadata`, `spec`, `status`, and metadata fields `name`, `namespace`, `generation`, `uid` (`ManifestReader.cs:36-37`).
- Many valid values depend on the Namespace's contents and are not static: the Connection a Device names, its Device Type, the Data Points, Actions and Events a Flow node references. `Validate` receives a `NamespaceSnapshot` for exactly this reason (`IResourceKind.cs:31`). A static JSON Schema could describe the field shapes. Autocomplete for these values needs data that only the Namespace and the Engine hold.

## 4. Integration config schemas (ADR-0009)

**Declared.** The SDK has a declarative, flat schema type. A `ConfigSchema(IReadOnlyList<ConfigField>)` lists fields, each with a name, a type (`String`, `Integer`, `Number` or `Boolean`) and a `Required` flag. Unknown fields are rejected (`packages/sdk/Flownodes.Sdk/ConfigSchema.cs:8-37`, `:50-89`). An Integration declares `ConnectionConfig` (`packages/sdk/Flownodes.Sdk/IIntegration.cs:19`). Each `DeviceType` declares an `Address` and a `Config` schema (`packages/sdk/Flownodes.Sdk/DeviceType.cs:15-19`). The simulator is an example (`integrations/flownodes.simulator/SimulatorIntegration.cs:11-31`). `ConfigField` has no description, default, enum, range or nesting, so even exposed it would give autocomplete only names and types.

**Action and Event payloads** use real JSON Schema, which is optional: `ActionDefinition.Parameters` and `.Result` and `EventDefinition.Payload` are `JsonElement?` (`packages/sdk/Flownodes.Sdk/DataPointDefinition.cs:60-66`, `:73`). They are part of a Surface, which is derived from config at apply time, and they are not a static per-Integration schema.

**Lost at the Core boundary.** The Registry reaches Integrations through `IIntegrationSchemas` in Core (`packages/core/Core.Application/Integrations/IIntegrationSchemas.cs:68-76`). Its `IntegrationSchema` and `DeviceTypeSchema` carry only validator delegates, `Func<JsonElement, IReadOnlyList<SchemaProblem>>`, and Surface delegates (`IIntegrationSchemas.cs:48-63`). The Engine fills them by wrapping `ConfigSchema.Validate` in closures (`packages/engine/Engine.Infrastructure/Integrations/InstalledIntegrations.cs:26-38`), so the field lists never cross into the Registry. A host without the Engine gets `NoIntegrations`, which knows no Integration (`packages/core/Core.Application/Integrations/NoIntegrations.cs:6-10`; `packages/registry/Registry.Application/Bootstrap/DependencyInjection.cs:30`).

**Not exposed over HTTP.** The only Integration endpoint is `GET /api/v1/engine/integrations`, operation `ListIntegrations` (`packages/engine/Engine.Presentation/Integrations/ListIntegrations/Endpoint.cs:18-34`). Its `IntegrationDto` holds `folder`, `id`, `version`, `sdkVersion`, `deviceTypes` (names only), `status`, `reason` and `message` (`packages/engine/Engine.Presentation/Integrations/Contracts/IntegrationDto.cs:5-13`), and nothing of the Connection config, Device address or Device config schemas. It requires a membership in some Namespace (`packages/engine/Engine.Application/Integrations/ListIntegrations/Query.cs:21`).

## 5. Generation on reads, and how a stale Generation is reported (ADR-0007)

**Reads return it.** `GetResource`, `ListResources`, `GetNamespace` and `ListNamespaces` return `metadata: {name, namespace, generation, uid}` (`ResourceDto.cs:15`, `:35`, `:43`). The Status carries `observedGeneration` (`ResourceDto.cs:68-71`). A read can be negotiated as YAML (`GetResource/Endpoint.cs:31`), and its envelope is the one apply accepts, so an editor can round-trip a read back into an apply. Note that apply accepts `status` at the top level (`ManifestReader.cs:36`).

**Sending it back.** `metadata.generation` is an optional precondition on apply. It must be a whole number of at least 1, and it is ignored on delete (`ManifestReader.cs:234-247`). The repo has no ETag or `If-Match` support (grep found none).

**A stale one.**
- `ManifestState.FirstStalePrecondition` compares each given Generation with the stored one and returns **the first** mismatch only (`packages/registry/Registry.Application/Resources/ManifestState.cs:83-97`).
- The handler runs that check **after** validation, so a Manifest that is both invalid and stale gets the 422, not the 409 (`packages/registry/Registry.Application/Resources/ApplyManifest/Command.cs:96-112`).
- The error is `Error.Conflict("resource.generation_conflict", …)`. Its message reads "{Kind} '{name}' was applied at Generation {n}, but it is now at Generation {m}. Read it again and reapply.", or "…but it no longer exists." (`Errors.cs:19-25`).
- `ToResourceProblem` falls through to `ToProblem`, which yields `409` `application/problem+json` with `title` = the code and `detail` = the message, and no extension fields (`packages/registry/Registry.Presentation/Resources/Http/ResourceProblems.cs:10-16`; `packages/core/Core.Presentation/Http/Problems.cs:23-35`). A client gets the Kind, the name and both Generations only by parsing English text.
- In the OpenAPI document, the 409 is a plain `ProblemDetails` (`ApplyManifest/Endpoint.cs:50`; dump).
- A unit test covers the code (`tests/registry/Registry.UnitTests/Resources/ApplyManifest/HandlerTests.cs:225`).

## 6. Validation feedback an editor can use today

- **Dry run.** `POST /api/v1/registry/apply?dryRun=true` runs the full validation and returns, per Resource, `action`, the resulting `generation` and a `changes` diff (`path`, `op`, `from`, `to`) (`ApplyManifest/Endpoint.cs:54-83`). An editor can call it for server-side validation of the whole Namespace as the apply would leave it.
- **Rejection body.** `422` with `title: "manifest.invalid"` and an `errors` **array** of `{document, kind, name, path, message}` (`ResourceProblems.cs:13`, `:18-34`; `packages/registry/Registry.Application/Resources/ManifestRejected.cs:9-25`). `path` is dotted, such as `spec.config.units`, and `document` is the 0-based index, or `null` for a Resource the Manifest did not list but whose meaning it changes. The endpoint declares `.ProducesValidationProblem(422)` (`ApplyManifest/Endpoint.cs:51`), so the document gives this response `HttpValidationProblemDetails`, whose `errors` is a map of string arrays (dump). That is **not** the shape the server sends.
- **Unreadable Manifest.** `400` with `title: "manifest.unreadable"` (`packages/registry/Registry.Presentation/Resources/Http/ManifestFormat.cs:46-50`).

## What is missing for a Manifest editor with validation and autocomplete

Gaps only, with no design proposed.

1. **No schema per Kind's Spec.** Specs are `{}` in OpenAPI, and the Kinds validate imperatively, so there is nothing to export.
2. **No schema for the Manifest envelope.** The apply request body is `{}`.
3. **No list of Kinds.** The editor cannot discover Kind names and plurals, nor whether a Kind is namespaced.
4. **Integration config, Device address and Device config schemas are not reachable.** The SDK declares them, Core reduces them to delegates, and `ListIntegrations` returns only Device Type names. Even once exposed, `ConfigField` gives names, types and required only.
5. **No live values for references.** Connection, Device Type, Data Point, Action and Event names depend on the Namespace and on the Engine's derived Surfaces. Apart from listing Resources, no endpoint serves them for completion.
6. **The OpenAPI document is wrong in places.** The `Endpoint.Response` collision misdescribes at least five Registry operations and omits `AppliedDto` and `ChangeDto`. The 422 apply response is declared as `HttpValidationProblemDetails` but sent as an array of problems. A client generated from this document would be wrong.
7. **No OpenAPI document outside Development and none at build time.** The Web UI has no stable artifact to generate types from.
8. **The Generation conflict is not machine-readable.** It has no structured Kind, name, expected and actual fields, it reports only the first stale Resource, and validation errors mask it.

## Not verified

- The Engine's and Identity's schemas in a full-host OpenAPI document: I dumped only a Registry host. The collision of their nested `Response` records is inferred from the naming rule.
- Running the test host in Development with scope validation on fails at start-up with "Cannot resolve scoped service 'Wolverine.IMessageBus' from root provider". I turned validation off for the dump. I did not check whether the real `apps/Flownodes` host in Development hits the same error.
