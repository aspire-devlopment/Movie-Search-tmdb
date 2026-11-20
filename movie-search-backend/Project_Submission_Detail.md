# Project Submission Detail

## 1. Project summary

This repository contains a .NET 10 REST API for movie discovery. The API exposes a stable, camelCase JSON contract and retrieves movie and TV data from The Movie Database (TMDB). It has no database, authentication system, frontend, or deployment configuration; those were outside the assignment scope.

The implementation follows the supplied layered structure and request-handler pattern. The core P0 movie search and pagination flow is present, along with P1 automated tests, P2 movie details and videos, and P3 TV search and short-lived caching. Additional work includes input validation, problem responses, URL-segment API versioning, CORS restrictions, per-IP rate limiting, asynchronous error-file logging, and Swagger/OpenAPI support.

## 2. Architecture and request flow

```text
HTTP request
  -> versioned ASP.NET Core controller (validation and HTTP status)
  -> request-handler factory and handler (use-case orchestration)
  -> domain-facing IMovieService
  -> in-memory search cache, or ITmdbClient
  -> typed HttpClient -> TMDB
  -> TMDB DTO -> domain entity -> API response DTO -> JSON
```

Cancellation is passed from `HttpContext.RequestAborted` through the handler and service to the HTTP client. TMDB transport types and naming are kept in Infrastructure; the public API uses its own DTOs and mapping. This separation limits the effect of upstream schema changes on the frontend contract.

### Project folders

| Path | Purpose |
|---|---|
| `src/MovieSearchCase.WebApi/Controllers` | HTTP routes, parameter binding, request validation, and status/problem responses. `Controllers/V1` contains the v1 movie and TV controllers. |
| `src/MovieSearchCase.WebApi/Handlers` | Use-case handlers and factories that call service interfaces and map results to API response DTOs. |
| `src/MovieSearchCase.WebApi/Models` | Public response models. Movie details are separate from the base Movie model so added detail fields do not alter search/trending responses. |
| `src/MovieSearchCase.WebApi/Mappers` | Maps domain entities to public API DTOs. |
| `src/MovieSearchCase.WebApi/Application` | Cross-cutting web concerns: API defaults, CORS, Swagger, versioning, rate limiting, problem responses, request logging, and error-file logging. |
| `src/MovieSearchCase.Domain/Entities` | Provider-independent movie, TV, video, and paging data structures. |
| `src/MovieSearchCase.Domain/Interfaces` | Service and TMDB client contracts used across layers. |
| `src/MovieSearchCase.Domain/Exceptions` | Typed domain/application exceptions and error categories. |
| `src/MovieSearchCase.Infrastructure/Clients` | Typed TMDB HTTP client, provider response models, configuration, and provider-to-domain mapping. |
| `src/MovieSearchCase.Infrastructure/Services` | Movie service and bounded in-memory search caching. |
| `src/MovieSearchCase.Infrastructure/Extensions` | Dependency injection and validated TMDB client registration. |
| `src/MovieSearchCase.Shared` | Small shared asynchronous request-handler contract and options helpers. |
| `tests/MovieSearchCase.WebApi.Tests` | Controller input-validation and handler response-mapping tests. |
| `tests/MovieSearchCase.Infrastructure.Tests` | Service/cache and TMDB HTTP-client tests using fake responses. |

`Program.cs` wires dependencies and middleware. Swagger is enabled only in Development. Development secrets can be supplied with the ignored `appsettings.Development.local.json` file or user secrets/environment configuration; the local file must not be committed.

## 3. Delivery priorities: P0 through P3

| Priority | Work | Reasoning |
|---|---|---|
| P0 | Movie search, paging envelope, validation, and documented error behavior. | This is the required user journey and the frontend-facing contract, so it was completed first. |
| P1 | Unit tests for handlers/controllers, service cache behavior, and TMDB client mapping/error cases. | These cover the boundaries where provider JSON, mapping, and input rules can break without requiring a live TMDB account. |
| P2 | Movie details and video endpoints. | These were requested as follow-on functionality and fit the existing handler/service/client layering. |
| P3 | TV series search and in-memory cache for movie and TV search pages. | TV search reuses the paging rules and is additive; cache reduces repeated provider calls. |

The implementation did not add a database, user accounts/authentication, frontend, deployment pipeline, distributed cache, or production hosting configuration. Those were excluded by the assignment scope or need environment and product decisions. The P3 TV route is additive because the original contract described movie responses only.

## 4. API contract and endpoints

Local base URL: `http://localhost:5080/api/v1` (HTTPS may also be available depending on the launch profile). Paths are case-insensitive in ASP.NET Core, but these lowercase paths match the contract. Successful JSON fields are camelCase. Date fields are ISO `YYYY-MM-DD` strings or `null` when absent. Errors use `application/problem+json`.

| Method and path | Purpose | Typical responses |
|---|---|---|
| `GET /movies/trending` | This week's trending movies. | `200` array of Movie objects; upstream failure `503`. |
| `GET /movies/search?query={text}&page={number}` | Movie search, default page 1. | `200` paging envelope; invalid query/page `400`; upstream failure `503`. |
| `GET /movies/{id}` | Movie details including runtime, tagline, genres. | `200` MovieDetails; invalid/non-positive ID `400`; unknown movie `404`; upstream failure `503`. |
| `GET /movies/{id}/videos` | Video metadata for a movie. | `200` array (empty if no videos); invalid ID `400`; unknown movie `404`; upstream failure `503`. |
| `GET /tv/search?query={text}&page={number}` | Additive TV series search using the same page rules. | `200` TV paging envelope; invalid query/page `400`; upstream failure `503`. |

Movie search and TV search require a nonblank query, trim surrounding whitespace, reject control characters, and cap query length at 200 characters. Page is optional, defaults to 1, and must be an integer from 1 through 500. A no-match search is still `200` with an empty `results` array. Movie IDs must be positive integers.

### Sample requests

**Trending movies**

```http
GET http://localhost:5080/api/v1/movies/trending
Accept: application/json
```

**Search movies**

```http
GET http://localhost:5080/api/v1/movies/search?query=batman&page=1
Accept: application/json
```

Expected shape (actual titles and counts are supplied by TMDB and can change):

```json
{
  "page": 1,
  "totalPages": 12,
  "totalResults": 231,
  "results": [
    {
      "id": 268,
      "title": "Batman",
      "overview": "A sample movie overview.",
      "posterPath": "/sample-poster.jpg",
      "backdropPath": "/sample-backdrop.jpg",
      "voteAverage": 7.2,
      "releaseDate": "1989-06-21"
    }
  ]
}
```

**Movie details and videos**

```http
GET http://localhost:5080/api/v1/movies/268
GET http://localhost:5080/api/v1/movies/268/videos
Accept: application/json
```

**TV search**

```http
GET http://localhost:5080/api/v1/tv/search?query=breaking%20bad&page=1
Accept: application/json
```

TV results use `name` and `firstAirDate`; movie results use `title` and `releaseDate`.

### Error examples

Blank query or an invalid page returns `400 Bad Request` with a problem body similar to:

```json
{
  "type": "about:blank",
  "title": "Invalid request",
  "status": 400,
  "detail": "The 'query' parameter is required and must not contain only spaces."
}
```

Unknown movie IDs return 404. TMDB connectivity, timeout, and invalid upstream response failures map to 503. Unexpected failures return a generic 500 response with a trace identifier; internal exception details are logged rather than returned to callers. Rate-limit rejection returns 429 with a `Retry-After` header.

## 5. How to run and test with Swagger or Postman

### Start the API

1. Install the .NET 10 SDK.
2. Configure a TMDB API Read Access Token as `TmdbOptions:AccessToken` using the ignored local development settings file, user secrets, or an environment variable. Never put the token in a Postman collection or source-controlled file.
3. From the repository root run:

```powershell
dotnet run --project src/MovieSearchCase.WebApi
```

4. Open `http://localhost:5080/swagger` in Development. Select the documented v1 API document if the UI offers a document selector, expand an operation, choose **Try it out**, fill query/path parameters, and choose **Execute**. Swagger's generated request body editor is only shown for operations that declare a request body.

### Postman setup

Create an environment variable `baseUrl` with value `http://localhost:5080/api/v1`. Add requests using the methods and paths in the API table. For GET requests, put `query` and `page` in the **Params** tab, or include them in the URL. Set `Accept: application/json`. No request JSON body is defined by the current contract: all current endpoints are GET operations and receive their inputs in the URL. Sending JSON in a GET body is not supported by these routes. If a JSON-body search is needed, add a separately specified POST endpoint with a request DTO and contract before expecting Swagger/Postman to accept such a body.

Example Postman calls:

```text
GET {{baseUrl}}/movies/search?query=batman&page=1
GET {{baseUrl}}/movies/268
GET {{baseUrl}}/movies/268/videos
GET {{baseUrl}}/tv/search?query=breaking%20bad&page=1
```

For a negative validation check, call `GET {{baseUrl}}/movies/search?query=%20%20&page=1`; expected status is 400. For the page limit check use `page=501`; expected status is 400.

### Automated tests

From the repository root:

```powershell
dotnet test MovieSearchCase.slnx
```

The Web API tests cover handler output mapping/paging and controller validation. Infrastructure tests use mocks and controlled HTTP responses to cover service delegation/cache behavior and TMDB client mapping/error behavior without relying on live TMDB. The repository submission notes record a prior run with 19 passed and 0 failed; rerun the command on the current checkout to confirm its current state.

## 6. Implementation choices and controls

### Layering and dependency injection

Controllers depend on request-handler factories; handlers depend on the domain-facing `IMovieService`; the service depends on `ITmdbClient`. Implementations are registered through Web API and Infrastructure extension methods. The typed `HttpClient` centralizes the HTTPS base URL, bearer token, and 10-second timeout, while `IHttpClientFactory` manages handler reuse. This keeps controllers testable and makes provider behavior replaceable behind an interface.

### Caching

Successful movie and TV search pages are cached in `IMemoryCache` for five minutes using an absolute expiration. Keys include the normalized query and page, and separate prefixes prevent movie/TV collisions. The cache has a size limit of 1,000 entries, with each search page assigned a size of one. Failures are not cached so the service can recover as soon as TMDB does. Trending, details, and video calls are not cached. This is a small, process-local cache: it does not share entries across replicas and can return slightly stale search results during its TTL.

### CORS

CORS allows only configured exact HTTP(S) origins; wildcard, malformed, path-bearing, query-bearing, fragment-bearing, and credential-bearing origins are rejected during startup. Allowed methods are GET and OPTIONS; allowed request headers are Accept and Content-Type; `Retry-After` is exposed to browser clients. This matches the current GET-only API and supports preflight while limiting browser access. A production deployment must set the actual frontend origin(s). CORS is a browser policy, not authentication or a substitute for server-side access control.

### Rate limiting

A global fixed-window limiter allows 60 requests per remote IP per 60 seconds and queues no excess requests. Rejections return 429, a problem response, and `Retry-After`. No `X-Forwarded-For` value is trusted unless trusted proxy middleware is configured, which avoids spoofable client identity. Behind a reverse proxy, configure trusted forwarding before relying on per-client IP partitioning. This limiter is in-process; multiple replicas each have independent counters.

### Constants and configuration

Shared policy values live in `ApiDefaults`: API version (1.0), default page (1), maximum page (500), maximum query length (200), rate allowance/window (60/60 seconds), error log retention (14 days), error log queue capacity (4096), default log directory, and route templates. Cache policy values are in `SearchCacheDefaults`; TMDB trust host and timeout are in `TmdbOptions`. Keeping these values centralized reduces drift between endpoints and policy code. Configuration that differs by environment (token, CORS origins, log directory) remains in configuration instead of being hard-coded.

### Async behavior and error handling

Provider I/O is asynchronous throughout; no synchronous wait is used. Request cancellation is propagated to the HTTP call, and cancellation caused by a disconnected caller is not logged as an application error. TMDB calls have a 10-second timeout. Known upstream failures become 503; controller validation and missing entities get appropriate 400/404 responses; unexpected exceptions return a generic 500 problem response. Error and critical log records are queued and written by a background service to daily JSONL files, with old files removed after 14 days. The bounded queue keeps disk writes off the request thread, though queue saturation and local disk availability remain operational concerns.

### Why exception middleware is used

`RequestLoggingMiddleware` wraps the request pipeline and acts as the centralized exception boundary. It maps known application/problem exceptions to their intended status, maps upstream HTTP failures to 503, and converts unexpected exceptions to a generic 500 Problem Details response. This is a better fit for cross-cutting exception handling than repeating broad `try/catch` blocks in every controller: it keeps error media types and response shape consistent, reduces duplicated code, and prevents raw exception details from being returned to clients. Controllers still return explicit 400/404 responses for expected validation and not-found cases, where the endpoint has useful context. The middleware also avoids trying to rewrite a response after headers/body have already started; it logs and rethrows that case. A caller disconnect cancellation is treated as normal cancellation rather than an application fault.

The middleware is named `RequestLoggingMiddleware`, though its important responsibility is exception handling as well as error logging. A future cleanup could rename it to `ApiExceptionHandlingMiddleware` and use a separate request logging/telemetry middleware if access logs are needed. ASP.NET Core's built-in Problem Details services can remain responsible for response construction, while middleware owns the consistent exception-to-status mapping.

### Error logging choices and follow-up

The current logger records Error and Critical level events, serializes entries as JSON lines, and sends them to a bounded in-process channel. A hosted background writer appends daily files and removes files older than 14 days. This keeps synchronous disk writes out of controller/request execution and gives a simple local diagnostic trail. The middleware logs the exception server-side while returning a generic response, which preserves diagnostic information without exposing stack traces in the HTTP response.

For production, use structured logging with request/trace identifiers and centralized collection so an error can be connected to its request across multiple instances. Define redaction rules for access tokens, authorization headers, query strings that could contain user data, and any personal information before expanding logs. Monitor the bounded queue for dropped entries, plus disk/write failures and retention. Keep client-facing errors generic; log detail only in a controlled server-side sink. The current file logger is useful for development and a self-contained assignment, but it is not a complete observability or audit system.

### Validation and injection prevention

Movie/TV search validates blank input, trims query text, rejects control characters, caps length, applies a bounded integer page, and URL-encodes the query before building the TMDB request. Movie IDs are integer-bound and must be positive. The TMDB base URL must be HTTPS on `api.themoviedb.org`, and the access token is sent in the Authorization header rather than a URL. There is no SQL/database layer, so SQL injection is not applicable; data is not interpolated into SQL. URL encoding and a fixed trusted upstream host limit path/query manipulation. Normal framework JSON binding is used for responses; there are currently no JSON request-body endpoints.

## 7. Tests and rationale

The test suite is divided by architectural boundary:

- `SearchMoviesHandlerTests` verifies the paging envelope, mapped movie results, and preservation of an empty search response.
- `GetTrendingMoviesHandlerTests` verifies trending response mapping.
- `MoviesControllerTests` verifies request validation and relevant controller behavior without making network calls.
- `MovieServiceTests` verifies delegation and successful search-page caching, including normalization of cache keys across query casing.
- `TmdbClientTests` uses fake HTTP responses to verify upstream JSON parsing, mapping, and failure handling.

This arrangement gives fast deterministic feedback on business mapping, validation, caching, and provider integration. It does not replace end-to-end HTTP tests: routing, API-version matching, CORS middleware, rate-limit behavior, serialization, and real cancellation should also be covered at integration level.

## 8. Contract questions and possible changes

### Changes and additions made beyond the original contract

The required movie contract was kept intact: movie search remains `GET /api/v1/movies/search` with query/page parameters and the specified paging envelope; the movie response field names and casing remain as defined. The following are implementation choices or additions beyond that core contract:

| Change/addition | What was done | Why | Contract and compatibility effect |
|---|---|---|---|
| Additive TV search | Added `GET /api/v1/tv/search` with the same paging rules. TV results use `name` and `firstAirDate`, which match TMDB's TV concepts, rather than pretending TV has movie `title` and `releaseDate` fields. | This was a P3 stretch feature and follows TMDB's parallel TV search capability while keeping TV types separate from movie types. | Existing movie clients are unaffected. This endpoint and its response shape were not specified in the original contract, so consumers should review/approve it before relying on it as a stable API. |
| Stricter input bounds | Search query is trimmed, limited to 200 characters, and rejects control characters. Page is constrained to 1–500, and IDs must be positive. | Bounds reduce malformed or excessively large inputs, align with documented paging rules, and protect the upstream request path. | Valid ordinary requests retain the same shape. Inputs outside these added bounds receive 400; document the limits for all clients. |
| Explicit operational responses | Added a per-IP 60 requests/60 seconds fixed-window limit with 429 and `Retry-After`, and exact-origin CORS policy. | Rate limiting limits accidental bursts and upstream usage; CORS allows the configured browser frontend origin. | These controls can reject or block requests that were previously unrestricted. Configure production origins and proxy identity correctly; CORS does not replace authentication. |
| Search cache | Added a five-minute bounded in-process cache for successful movie/TV search pages. | Avoids repeating identical provider calls and reduces latency/quota use. | No JSON contract change, but results can be up to five minutes stale. Cache is per process and does not share across replicas. |
| Unified problem handling | Errors use `application/problem+json`; upstream failures map to 503 and unexpected failures to generic 500 responses. | Provides consistent client behavior without disclosing internal exception details. | The contract specified common status codes and a problem shape, but not every detail or trace field. Clients should use status and documented problem fields rather than depend on undocumented wording. |

No JSON-body search endpoint was added. All specified operations are GET requests, so Swagger correctly has no JSON body editor for them. Adding a POST alternative would change the API surface and should require an agreed route, request DTO, validation rules, and compatibility plan.

API version 1.0 is the only implemented version, and URLs carry `v1`. Version 2 is not implemented; it should not be advertised as selectable/working in Swagger until there is a real v2 contract and implementation. The default version setting is 1.0, but callers should continue to include the explicit version segment.

The contract also leaves some details open: TV response ownership/approval; exact error `type`, trace identifier, and field-level validation shape; whether movie search should ever have a POST form; and how fresh search results need to be. These should be decided with the frontend/API owner and documented in OpenAPI before clients depend on them. For those reasons, the implementation keeps the required movie routes stable and treats TV and operational features as additive, documented behavior.

The documentation updates in this submission did not alter endpoint behavior or change the API contract; they explain the choices already present in the code.

## 9. Tradeoffs and current limitations

- **Local memory cache:** simple and bounded, but not shared across processes; no invalidation mechanism beyond TTL and no request coalescing during cache misses.
- **Fixed-window IP limiter:** easy to operate, but can allow a burst around window boundaries; counters are per process and proxy-aware identity requires trusted forwarding configuration.
- **File-based error sink:** self-contained and asynchronous, but lacks centralized search, alerting, rotation controls beyond daily naming/retention, and robust monitoring for queue overflow or disk failures.
- **No retry/circuit breaker:** avoids retry amplification and hidden latency but leaves transient upstream failures visible as 503. Resilience policy needs explicit retry budgets and honoring provider throttling before adoption.
- **No integration tests:** unit tests are deterministic, but do not exercise the full ASP.NET Core pipeline or a real external deployment.
- **No authentication, persistence, or deployment:** appropriate to the assignment, but unsuitable for a private or production service without product requirements and operational setup.
- **Swagger development-only:** limits accidental API discovery in production but means deployed API documentation needs an explicit publishing strategy.
- **Live TMDB dependence:** success data requires valid credentials and network access; provider quota, availability, and data changes affect responses.
- **Current GET-only contract:** Swagger does not show a JSON body editor because none of these endpoints accepts a body. A POST search endpoint would be an additive API design change, not a Swagger toggle.

## 10. Scalability and cache evolution

The API is mostly stateless: request data flows through handlers and services, while movie and TV data remains owned by TMDB. This makes it possible to run multiple API instances behind a load balancer. The current process-local memory cache and rate limiter are the main exceptions. Each instance has its own cached pages and request counters, so requests routed to different instances do not share cache hits or rate-limit state.

### Current cache: `IMemoryCache`

Movie and TV search pages use normalized query/page keys, a five-minute absolute TTL, and a maximum of 1,000 entries. Only successful responses are cached. This is a good fit for a small, single-instance service: it is fast, bounded, built into ASP.NET Core, and has no additional infrastructure cost. The tradeoffs are that entries disappear on restart, instances do not share entries, data can be up to five minutes old, and simultaneous requests for the same uncached query can all call TMDB.

### Redis or another distributed cache

Consider Redis when the service runs multiple replicas and shared cache hits would reduce TMDB calls, or when measured provider traffic/latency justifies the added dependency. Redis can share search results between instances and preserve cache entries through an individual API process restart. Keep a cache abstraction so the service does not depend directly on Redis APIs, and retain the current key intent: endpoint type + normalized query + page. Add a cache namespace/version to keys so changes to serialized response shapes do not reuse incompatible entries.

Before rollout, choose TTL and freshness expectations, serialization format, connection timeout, behavior when Redis is unavailable, and whether stale successful data may be served during a TMDB outage. Continue to avoid caching provider failures by default. Add hit/miss/eviction/latency metrics and request coalescing (single-flight) so identical misses do not stampede TMDB. A Redis outage should have a deliberate fallback, such as briefly falling back to local memory or bypassing cache with bounded upstream concurrency; it should not cause unbounded retries or hang requests.

Redis is not automatically the right next step for a small single-instance API. It adds operating cost, network latency, credential and network security requirements, serialization/versioning work, and another service to monitor. Measure cache hit rate, TMDB request volume, latency, quota pressure, and replica count first. Keep the bounded local cache until shared state solves a demonstrated need.

### Scaling beyond the cache

- **Rate limits:** for consistent per-client limits across instances, enforce quotas at a gateway or use shared rate-limit state. Trust forwarded client IP headers only from explicitly configured reverse proxies.
- **TMDB protection:** additional API replicas can increase provider traffic. Set concurrency budgets, honor TMDB `Retry-After`, and use restrained retry/circuit-breaker policies for idempotent GET requests.
- **Async requests:** keep asynchronous I/O, cancellation propagation, and bounded timeouts so slow upstream responses do not tie up request threads indefinitely.
- **Logs and telemetry:** local daily files are hard to correlate across replicas. Use centralized structured logs, metrics, and distributed traces before multi-instance operation.
- **Capacity decisions:** measure request latency, cache hit rate, upstream call volume, 429/5xx rates, memory use, log queue drops, and dependency failures before choosing infrastructure or scale targets.

## 11. Recommendations for future work

1. **Before deployment:** configure production secrets, allowed hosts, exact CORS origins, TLS termination, and trusted reverse-proxy forwarding.
2. **Add integration coverage:** test routing/versioning, CORS, rate limits, response serialization, problem media types, and request cancellation through the actual ASP.NET Core pipeline.
3. **Protect the upstream:** add measured concurrency limits, honor TMDB `Retry-After`, and consider bounded retries/circuit breaking for idempotent GETs.
4. **Share cache only when useful:** if multiple replicas or measured TMDB load justify it, adopt Redis or another distributed cache. Define versioned keys, TTL/freshness, outage fallback, stale-on-error behavior, and cache-stampede protection before migration.
5. **Share rate-limit state when needed:** use a gateway or distributed limiter if per-client quotas must hold across replicas.
6. **Add operational visibility:** health checks, structured metrics, distributed tracing, dashboards, and alerts for latency, 429/5xx rates, TMDB availability, cache hit rate/pressure, and dropped log records.
7. **Centralize production logs:** replace local files with a managed sink that has access controls, retention, and monitoring.
8. **Publish a reviewed contract:** have the frontend/API owner approve the additive TV response and publish OpenAPI documentation with examples and error schemas.
9. **Add CI safeguards:** build, test, format, dependency vulnerability, and secret scanning checks; resolve NuGet `NU1507` by mapping package sources or using one approved source.
10. **Add JSON-body search only if needed:** define a POST route and request schema with consumers first, and retain the current GET endpoint for compatibility.

