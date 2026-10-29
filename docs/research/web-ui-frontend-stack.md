# Frontend stack for the built-in web UI

Research for issue #99, feeding the grilling ticket #100 under the map #98 "Web UI ready". It compares options and does not choose one.

**Question.** How do a TypeScript single-page app (React, Svelte or Vue), Blazor WebAssembly, and server-rendered Razor with htmx compare for the built-in web UI? The comparison covers bundle size and load time, live updates, a read-only Flow graph, a typed client from the OpenAPI document, a schema-driven Manifest editor, build-tooling cost, and how well coding agents work with each.

**Constraints already decided (map #98).** The Flownodes host serves the UI from the same origin as the API, with no Node.js runtime in production. Technical users come first. A Raspberry Pi is the smallest target (ADR-0001). A Flow is shown read-only as a graph and edited through the Manifest editor. A Web API feature the UI needs, such as a push channel or schemas, belongs to every client.

**Repo facts that matter here** (checked on `main` at `78229e3`):

- The host already uses `Microsoft.AspNetCore.OpenApi` 10.0.13 and Scalar. It maps `/openapi/v1.json` and `/scalar` **in Development only** (`packages/core/Core.Presentation/Bootstrap/HostConfiguration.cs`).
- The Web API has no push channel yet: there is no SSE, WebSocket or SignalR code.
- `flowctl` calls the API through hand-written routes (`apps/Flowctl/Routes.cs`), not a generated client. Manifests travel as `application/yaml` or `application/json`.
- No JSON Schema for any Kind, or for Integration config, is served yet. A schema-driven editor needs one, whichever stack is chosen.
- ADR-0026 already says the web UI signs in through **authorization code with PKCE**, which is the browser-held-token pattern.
- CI already runs a codegen step that starts the host with dummy connection strings (`.github/workflows/ci.yml`, "Generated handler code is current").

**Sources.** Microsoft Learn (ASP.NET Core 10 docs and API reference), official project sites (htmx.org, reactflow.dev, svelte.dev, vite.dev, openapi-ts.dev), project READMEs on GitHub, the npm and NuGet registries, and IETF RFC 10017. Measurements marked **[measured]** were taken on 2026-10-29 on macOS with .NET SDK 10.0.403, Node 26.12.1 and Vite 8.3.5. Claims marked **inference** have no primary source behind them.

## Summary table

| Criterion | TypeScript SPA (React / Vue / Svelte + Vite) | Blazor WebAssembly (.NET 10) | Razor (server-rendered) + htmx |
|---|---|---|---|
| First-load JS/Wasm, empty app | React 68 KiB, Vue 26 KiB, Svelte 13 KiB gzip [measured] | ~2.1 MiB Brotli, trimmed, without relinking [measured]; AOT "about twice" | htmx ~16 KiB (htmx.org) + HTML per page |
| Work on the Pi | Serves static files only | Serves static files only | Renders HTML for every interaction |
| Live updates | SSE / SignalR JS client; token in query string or cookie | SignalR .NET client or SSE via JS; same token issue | htmx SSE extension swaps server-rendered HTML; cookie auth fits naturally |
| Read-only Flow graph | React Flow / Svelte Flow (xyflow, MIT); Vue Flow (separate project, MIT); layout via dagre or elkjs | Z.Blazor.Diagrams (MIT), or JS interop to a JS library | Server-side SVG (no maintained .NET layout library found) or a JS island |
| Typed client from OpenAPI | openapi-typescript + openapi-fetch (types only, ~3 KiB runtime), hey-api, orval, Kiota | Kiota or NSwag in C# | Not needed: pages call in-process. But that sidesteps the public API |
| Manifest editor | Monaco + monaco-yaml, or CodeMirror 6 + codemirror-json-schema | BlazorMonaco wraps Monaco; monaco-yaml needs JS-side worker setup | Same JS editors as islands, so a JS bundle step comes back |
| Build tooling | Node in dev and CI only; second toolchain (npm, ESLint, Prettier) | `dotnet build` only; `wasm-tools` workload (Emscripten) for relinking/AOT | `dotnet build` only, until a JS editor or graph library needs bundling |
| Agent support | llms.txt for React, Vue, Svelte, Vite, xyflow; Svelte MCP server | Microsoft Learn MCP server; repo analyzers apply | No llms.txt; htmx 2 vs 4 split |

The sections below give the detail and sources.

## 1. Bundle size and load time

### Measurements [measured]

These are default templates with no app code. JS and CSS are gzip -9, which is close to what Vite reports. Times are pure transfer time for the compressed bytes, with no latency or TLS. At 1 Mbit/s, 1 KiB takes about 8 ms.

| Bundle | Compressed | At 1 Mbit/s | At 10 Mbit/s |
|---|---|---|---|
| Svelte 5.57 `svelte-ts` template | 13.4 KiB gz | 0.1 s | <0.1 s |
| Vue 3.5.44 `vue-ts` template | 26.3 KiB gz | 0.2 s | <0.1 s |
| React 19.3.1 `react-ts` template | 68.2 KiB gz | 0.6 s | 0.1 s |
| React + `@xyflow/react` 12.12 (empty flow, with CSS) | 123 KiB gz | 1.0 s | 0.1 s |
| htmx 2.0.11 (bundled) | 18 KiB gz | 0.15 s | <0.1 s |
| `@microsoft/signalr` 10.0.11 client | 14 KiB gz | 0.1 s | <0.1 s |
| `openapi-fetch` 0.17 | 3 KiB gz | <0.1 s | <0.1 s |
| `@dagrejs/dagre` 3.1.3 layout | 16 KiB gz | 0.1 s | <0.1 s |
| `elkjs` 0.12.1 (bundled build) | 434 KiB gz | 3.6 s | 0.4 s |
| CodeMirror 6 + `@codemirror/lang-yaml` + `codemirror-json-schema` | 310 KiB gz | 2.5 s | 0.25 s |
| Monaco 0.57 + `monaco-yaml` 5.5.1, main thread | 1,176 KiB gz | 9.6 s | 1.0 s |
| Same, including all workers Vite emitted | 3,179 KiB gz | 26 s | 2.6 s |
| Blazor WASM `blazorwasm` template, Release, trimmed, `_framework` only | 2,147 KiB Brotli (+141–243 KiB ICU shard) | 17.6 s (+1.2–2 s) | 1.8 s |
| Same with `InvariantGlobalization=true` (no ICU) | 2,123 KiB Brotli | 17.4 s | 1.7 s |

Notes on the measurements:

- **Blazor.** The template was published without the `wasm-tools` workload, because installing it needs an administrator shell. The SDK warned: "Publishing without optimizations… we strongly recommend using `wasm-tools` workload". The figure therefore excludes runtime relinking, which Microsoft says "trims unused runtime code and thus improves download speed", with a reduction that is "particularly dramatic when disabling globalization" ([runtime performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/webassembly-runtime-performance?view=aspnetcore-10.0)). Read the 2.1 MiB as an upper bound for a trimmed, interpreted build. The template's Bootstrap CSS is excluded. The publish emitted 36 `.wasm` files, each with `.br` and `.gz` variants.
- **Monaco.** The Monaco figure imports `monaco-editor` whole, which pulls in every language and its worker. An app that registers only YAML and the editor worker loads less. The 1.2–3.2 MiB range brackets the cost. **Not measured:** a Monaco build trimmed to YAML only.
- **Real apps.** Expect app code, a router, a component library and the client to add to every row. Inference: the gap between the JS frameworks shrinks once the graph and editor libraries are added, because those dominate. The gap to Blazor's ~2 MiB runtime floor does not.

### What the primary sources say

- **Blazor WebAssembly.** "Download size is larger, and components take longer to load" and "Small payload size with fast initial load time" is ❌ for WebAssembly ([hosting models](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-10.0)). The Release build is IL-trimmed and "statically compressed during publish" with Brotli and gzip ([host and deploy](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-10.0)).
- **AOT** improves runtime speed "at the expense of a larger app size… most AOT-compiled apps are about twice the size of their IL-compiled versions". AOT "doesn't trim out managed DLLs" and "usually takes several minutes on small projects" ([build tools and AOT](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-10.0)). `WasmStripILAfterAOT` removes IL for compiled methods to shrink `_framework` ([runtime performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/webassembly-runtime-performance?view=aspnetcore-10.0)). Without AOT, code runs on an IL interpreter with partial JIT (the "Jiterpreter"), and near-native speed needs AOT ([hosting models](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-10.0)).
- **.NET 10 Blazor changes.** Framework assets can be preloaded through `<link rel="preload" id="webassembly" />`. Client-side fingerprinting is opt-in for standalone apps. `blazor.boot.json` is inlined into `dotnet.js` ([ASP.NET Core 10 release notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0)). The release notes read here give no size-reduction numbers.
- **Mobile memory.** The default `EmccMaximumHeapSize` is 2 GiB, which "may be too large" for some mobile browsers, especially Safari on iOS ([host and deploy](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-10.0)).
- **Serving from the host (all options).** `MapStaticAssets` compresses assets at build time (gzip in development, gzip and Brotli on publish). It fingerprints them with SHA-256, serves fingerprinted assets as `immutable`, and sets content ETags. It applies only to files "resolved as being under `wwwroot`" at build time, or linked into it with a `<Content Link=…>` item, so a Vite `dist/` must be placed or linked there to benefit. It does not minify. It "eagerly loads the precomputed metadata", which costs memory ([static files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0)).

### Raspberry Pi

Inference, not measured on a Pi:

- **SPA and Blazor WebAssembly.** The Pi serves precompressed static files once, then only API and push traffic. The rendering CPU cost lands on the viewer's device. Blazor's interpreted .NET is the heaviest client on a weak viewer, such as a Pi used as a kiosk. JS frameworks run on the browser's JIT.
- **Razor + htmx.** Every navigation and every swap renders HTML on the Pi, next to the Engine's Orleans silo. The payload per update is small, but CPU per request is spent on the Pi. Live updates push rendered fragments, not JSON, so each pushed value means a server-side render per subscribed client.
- **Repeat visits.** After the first load, fingerprinted assets are cached as `immutable`. The cold-load gap between options matters mostly for the first visit and after each upgrade.

## 2. Live updates

- **Server side, all options.** .NET 10 adds `TypedResults.ServerSentEvents`, which streams `SseItem<T>` with event type, id and data from an `IAsyncEnumerable` ([release notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0)). SignalR is the other built-in option.
- **The authentication catch for the SPA and Blazor.** "Browsers can't set custom headers, such as `Authorization`, on the WebSocket and Server-Sent Events APIs. Because of this browser limitation, SignalR sends the token as a query string parameter for those transports." The server must read `access_token` from the query, and "many servers log query string values" ([SignalR authn/authz](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0)). ADR-0026 keeps the JWT in the browser, so a browser-run client needs one of three things:
  - a query-string token;
  - a `fetch`-based SSE reader, which can send headers (inference);
  - a cookie.
- **SPA.** The client is the `@microsoft/signalr` JS client (14 KiB gzip [measured]) or a few lines of SSE code. Reactive stores in Svelte, Vue or React fit a stream of Data Point values (inference).
- **Blazor WebAssembly.** It uses the SignalR .NET client, or calls `EventSource` through JS interop. Both are browser transports, so the same header limit applies. This is inference from the SignalR page, which says the limit is a browser API constraint.
- **htmx.** The SSE extension (`htmx-ext-sse` 2.2.4) opens an `EventSource`. `sse-swap` swaps named events into the DOM, and `hx-trigger="sse:<name>"` can trigger a re-fetch. The extension adds its own exponential-backoff reconnect on top of the browser's. Its docs do not cover headers or authentication ([htmx SSE extension](https://htmx.org/extensions/sse/)). Inference: with server-rendered pages the natural sign-in is a cookie session, under which `EventSource` sends credentials same-origin and the header limit disappears. The server pushes HTML, so the push channel is UI-specific. That conflicts with the map's note that a push channel belongs to every client: `flowctl` and the Control Center would need a JSON stream as well.
- **Version caveat.** htmx 4.0 "has been released" but "is not currently marked as `latest` in NPM", and the team "will mark it `latest` at some point in 2027". npm on 2026-10-29: `latest` 2.0.11, `next` 4.0.0 ([htmx.org](https://htmx.org/)). Not verified: how htmx 4 changes the SSE extension.

## 3. Rendering a Flow as a read-only graph of Nodes

- **React Flow and Svelte Flow** come from the same xyflow project and are MIT-licensed. `@xyflow/react` 12.12.0 and `@xyflow/svelte` 1.7.0 were both published on 2026-09-24 (npm). "React Flow nodes are simply React components", so a custom Node per Node type is ordinary component code ([reactflow.dev](https://reactflow.dev/)). Read-only means turning off `nodesDraggable`, `nodesConnectable`, `elementsSelectable`, `panOnDrag` and the zoom props ([API reference](https://reactflow.dev/api-reference/react-flow)). There is no single read-only switch.
- **Vue Flow** (`@vue-flow/core` 1.48.2, MIT, last published 2026-01-28) is a separate community project, not part of xyflow (npm).
- **Layout.** React Flow says: "We have not implemented our own layouting solution yet". It points to dagre, d3-hierarchy, d3-force and elkjs ([layouting](https://reactflow.dev/learn/layouting/layouting)). `@dagrejs/dagre` 3.1.3 is MIT and 16 KiB gzip. `elkjs` 0.12.1 is licensed **EPL-2.0 OR GPL-3.0-or-later** and is 434 KiB gzip [measured, npm]. The licence is worth checking against Flownodes' AGPL-3.0. Inference: a Manifest probably carries no Node coordinates, so some auto-layout is required whichever stack is chosen.
- **Blazor.** `Z.Blazor.Diagrams` 3.0.4.1 is MIT, was last published 2026-03-02 and has about 2.0 M downloads (NuGet). The alternative is wrapping a JS library through JS interop, which brings back a JS dependency and a bundling decision (inference).
- **Razor + htmx.** One option is to render SVG on the server. The .NET layout library found, `Microsoft.MSAGL` 1.1.6, was last published 2023-02-23 (NuGet), so it looks unmaintained. The other option is to embed a JS graph library as an island. Inference: React Flow and Svelte Flow need their framework, so an htmx island would most likely use elkjs or dagre with hand-drawn SVG.

## 4. A typed client from the host's OpenAPI document

- **Producing the document in CI.** Adding `Microsoft.Extensions.ApiDescription.Server` generates OpenAPI at build time. It does so by "launching the app's entrypoint with a mock server", so startup code runs. Paths can be guarded on the entry assembly being `GetDocument.Insider`. `OpenApiDocumentsDirectory` sets where the file lands ([Generate OpenAPI documents](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)). The host already tolerates being started in CI for Wolverine codegen. Today it serves the document in Development only.
- **OpenAPI 3.1 quirks that hit generators.** 3.1 is the default in .NET 10. Nullable types become a `type` array containing `null`. By default, `int` and `long` "no longer include `type: integer`" and get a digits-only `pattern` instead, unless `NumberHandling` is `Strict` ([release notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0)). Inference: generators may type such fields as `string | number` until that is set.
- **TypeScript.** `openapi-typescript` 7.13.0 generates "runtime-free types", supports OpenAPI 3.0 and 3.1, and needs no Java ([openapi-ts.dev](https://openapi-ts.dev/introduction)). Its companion `openapi-fetch` is 3 KiB gzip [measured]. Alternatives are `@hey-api/openapi-ts` 0.99.0 (pre-1.0) and `orval` 8.45.0 (npm). Kiota also generates TypeScript ([Kiota overview](https://learn.microsoft.com/en-us/openapi/kiota/overview)).
- **Blazor.** Kiota (C#) or NSwag (`NSwag.MSBuild` 14.7.1) generates a C# client (NuGet). Inference: referencing module DTO types directly from a UI project would cross the module and layer boundaries of ADR-0018, so a generated client is the clean path here too. Not verified: Kiota's and NSwag's handling of .NET 10's 3.1 `pattern`-typed integers.
- **Razor + htmx.** No client is needed if pages call Application handlers in process. Inference: that makes the UI a second path beside the Web API. It contradicts the README ("It writes the same Resources through the same API") and would make the host's Presentation reach into modules, against ADR-0018. If pages instead call the API over loopback HTTP, a C# client is needed after all.

## 5. Manifest editor with schema-driven validation and autocomplete

- **Monaco + monaco-yaml** (MIT, 5.5.1, published 2026-06-03) provides "schema-based code completion and hovers, plus syntax and schema validation", mapping schemas to files through `fileMatch` and `uri`. "The web worker provides the core functionality", registered through `MonacoEnvironment.getWorker`. It "relies on dependencies in `node_modules`", so a bundler is required ([monaco-yaml](https://github.com/remcohaszing/monaco-yaml)). The README does not say which JSON Schema drafts it supports. It is heavy: 1.2–3.2 MiB gzip [measured, see §1].
- **CodeMirror 6** with `@codemirror/lang-yaml` 6.1.3 and `codemirror-json-schema` 0.8.1 (MIT) weighs 310 KiB gzip [measured]. `codemirror-json-schema` was last published 2025-04-21 (npm), so it is less actively maintained than monaco-yaml.
- **SPA.** Either editor is a direct npm dependency.
- **Blazor.** `BlazorMonaco` 3.5.0 (MIT, 2026-06-28, ~4.4 M downloads) wraps Monaco. Inference: monaco-yaml's worker and `configureMonacoYaml` call are JS-side, so Blazor needs a small JS module and probably a bundler for it, plus Monaco's weight on top of the .NET runtime.
- **Razor + htmx.** Same JS editors as an island. The bundler and `node_modules` requirement returns, so "no JS toolchain" no longer holds (inference).
- **All options.** The editor needs JSON Schemas for each Kind's spec and each Integration's config from the API. Neither exists yet (repo fact). ADR-0009's apply-time validation stays the authority, and editor validation is advisory.

## 6. Build-tooling cost in the .NET solution and in CI

- **SPA.** Node is needed at development and build time only. Vite 8.3.5 declares `engines.node` `^20.19.0 || >=22.12.0` (npm). CI adds `setup-node`, an npm cache and `npm ci`. The output must land under, or be linked into, the host's `wwwroot` before `dotnet publish` to get `MapStaticAssets` compression and fingerprinting ([static files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0)).
  - For local development, Vite's backend-integration guide has the backend proxy asset requests to the Vite dev server, or set `server.origin` ([Vite backend integration](https://vite.dev/guide/backend-integration)). The reverse, Vite proxying `/api` to the host, is also common (inference).
  - Visual Studio's `.esproj` JavaScript Project System can hold Angular, React or Vue projects inside a solution ([JS/TS in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/javascript/javascript-in-visual-studio?view=vs-2022)). Not verified: how well `.esproj` works under `dotnet build` on Linux CI. A plain MSBuild `Exec` target or a separate CI step is the conservative route (inference).
  - The repo's standards (analyzers that fail the build, ADR-0016; `scripts/format.sh`, ADR-0022; architecture tests) do not reach TypeScript. ESLint, Prettier or Biome, and `tsc`, would be a second set. The map already lists frontend standards as waiting on the stack.
- **Blazor WebAssembly.** Everything is `dotnet build`, `dotnet publish` and C#, so the analyzers, `format.sh`, `.editorconfig` and architecture tests apply. Relinking and AOT need the `wasm-tools` workload (Emscripten), installed per SDK. Python must be on `PATH` on Linux. AOT is publish-only and slow ([build tools and AOT](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-10.0)). CI gains a `dotnet workload install wasm-tools` step if relinking is wanted (inference). A JS editor or graph library adds JS interop and possibly a small bundling step.
- **Razor + htmx.** Same .NET-only toolchain, with htmx vendored or referenced as a static file. This holds until the Manifest editor or graph library needs `node_modules` and a bundler (§3, §5).

## 7. How well coding agents work in each

Most of this is inference: no primary source measures agent quality per framework. These facts are checkable:

- **llms.txt.** React (`react.dev/llms.txt`), Vue (`vuejs.org/llms.txt`), Svelte, Vite, React Flow and Svelte Flow all return HTTP 200. `htmx.org/llms.txt` returns 404 (checked 2026-10-29). Svelte publishes full, medium and small variants ([Svelte LLM docs](https://svelte.dev/docs/llms)).
- **Svelte MCP server.** Svelte ships an MCP server whose tools are "designed to help your agent write correct, robust Svelte code", using its docs and static analysis to fix common AI-generated mistakes ([Svelte MCP](https://svelte.dev/docs/mcp/overview)).
- **Microsoft Learn MCP server.** It gives agents search and full-article fetch over official .NET and ASP.NET docs, Blazor included ([Learn MCP](https://learn.microsoft.com/en-us/training/support/mcp)).

Inference:

- **TypeScript SPA.** React has by far the most public code, so agents produce idiomatic React most reliably. Svelte 5's runes are newer than much training data, which is why Svelte invests in llms.txt and an MCP server. A generated OpenAPI type layer plus `tsc` gives agents a fast compile-time check on API calls.
- **Blazor.** Agents get C# with this repo's strict analyzers and architecture tests, the same feedback loop as the backend. There is less public Blazor WebAssembly code, and JS-interop seams are a common source of errors.
- **Razor + htmx.** The markup is simple, but correctness lives in matching `hx-*` attributes, element ids and server fragments, which no compiler checks. Agents may mix htmx 2 and htmx 4 idioms while both are current.

## Facts the decision hinges on

1. **Cold load over a slow link.** Blazor WebAssembly's floor is about 2 MiB compressed before app code (17 s at 1 Mbit/s, measured without relinking). A JS framework's floor is 13–68 KiB. In every JS option the editor (0.3–3 MiB) and graph libraries dominate the total.
2. **Sign-in model.** The SPA and Blazor fit ADR-0026's authorization code with PKCE, at the cost of a query-string or `fetch`-based token for SSE/SignalR. Razor + htmx fits a cookie session, which RFC 10017 ranks as more secure ("BFF") than a browser-based client, the least secure of its three patterns. Choosing htmx means revisiting ADR-0026's web-UI consequence ([RFC 10017](https://datatracker.ietf.org/doc/draft-ietf-oauth-browser-based-apps/)).
3. **Same API as every client.** The SPA and Blazor consume the public Web API and its push channel, as `flowctl` and the Control Center do. Razor + htmx tends toward an HTML-only path and HTML-only pushes, unless pages call the API over HTTP.
4. **"No JavaScript toolchain" does not survive the Manifest editor.** Every option ends up with a JS editor (Monaco or CodeMirror) and probably a JS layout library. Blazor and htmx then carry a small JS build or vendored bundles, alongside their own stack.
5. **Where CPU is spent.** Razor + htmx renders on the Pi for every interaction and push. The SPA and Blazor push that cost to the viewer's browser.
6. **Standards reach.** Blazor and htmx stay inside the C# analyzers and architecture tests. An SPA needs a parallel TypeScript standard and CI job.

## Not verified

- Blazor WebAssembly size with `wasm-tools` relinking, and with AOT plus `WasmStripILAfterAOT`. The workload could not be installed without an administrator shell.
- Any timing on a real Raspberry Pi, or on a Pi used as a kiosk browser.
- A Monaco build trimmed to YAML only.
- htmx 4's SSE support and its migration impact.
- `.esproj` behaviour under `dotnet build` on Linux CI.
- How OpenAPI generators (openapi-typescript, Kiota, NSwag) handle .NET 10's `pattern`-typed integers.
- Which JSON Schema drafts monaco-yaml and codemirror-json-schema support.
