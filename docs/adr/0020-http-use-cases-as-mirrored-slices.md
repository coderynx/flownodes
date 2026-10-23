# HTTP use cases are slices mirrored across Application and Presentation

Each HTTP use case is one slice with the same relative path in two layers of its module. `<Module>.Application/<Feature>/<UseCase>/Command.cs` (or `Query.cs`) holds the request record and its handler, with an optional `Errors.cs` beside it for expected error codes and messages. `<Module>.Presentation/<Feature>/<UseCase>/Endpoint.cs` holds the `IEndpoint` that binds the request, authenticates the caller, dispatches through `Coderynx.MediatorKit` and maps the outcome to HTTP. We keep the layer split (ADR-0018) rather than putting both files in one directory, so the Application layer stays free of HTTP, and mirror the path so a use case is still found in one place per layer.

## The boundary between the two

- HTTP request and response records live inside the endpoint class. Route and query values are grouped in a request record with `[FromRoute]` and `[FromQuery]` members, bound through `[AsParameters]`.
- The mediator request accepts primitives, domain values or use-case input types, never HTTP DTOs, `ClaimsPrincipal` or ASP.NET types. The endpoint extracts the caller and maps HTTP input before dispatch; the handler owns use-case validation, ownership checks, business flow and database access, and calls domain methods for their invariants (ADR-0019).
- Handlers return `Coderynx.Functional` `Result` or `Option`, with `Ensure`, `Bind` and `Map` where a chain makes the flow clearer. The `Result<T>` holds domain data or a use-case result, never an HTTP response or DTO. The endpoint uses `Match` (or `Coderynx.Functional.WebApi`'s mapping) to translate the outcome into the route's status codes, bodies and headers.

## The HTTP surface

- Every endpoint hangs off one prefix, `/api/v1`, written once in `Core.Presentation` and applied when endpoints are mapped. A module maps `/registry/...`, never the version.
- Every endpoint names its operation (`.WithName(...)`), which becomes the OpenAPI `operationId` and a generated client's method name. A schema nested in another type is named after both, so two endpoints' `Response` records stay two schemas. An endpoint returning `IResult` declares what it returns with `.Produces<T>(...)`.
- The OpenAPI document is generated from the endpoints as mapped and never checked in.
- List endpoints share one shape: `offset` and `limit` page it (`limit` clamped), `sort` picks one of the endpoint's own keys (`400` naming the accepted ones for an unknown key), `desc` reverses it, and `q` filters. Errors are `application/problem+json`.

## Consequences

Architecture tests fail the build on a mediator request or handler outside a `Command.cs` or `Query.cs` two directories deep, on an `IEndpoint` outside an `Endpoint.cs` whose path has no Application counterpart, on `[FromRoute]` or `[FromQuery]` outside a record, on a mediator request or response carrying an ASP.NET or `ClaimsPrincipal` type, on a module's Presentation writing `"/api`, and on a mapped endpoint without a name. Because Presentation depends on Application and not the reverse, a handler cannot see an HTTP DTO at all. `.Produces<T>`, the list shape and problem documents are held by review.
