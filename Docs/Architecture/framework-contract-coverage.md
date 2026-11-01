# Framework architecture contract coverage

This ledger distinguishes architectural rules enforced by the dedicated architecture suite from runtime behavior verified by the ordinary unit and integration suites. A passing test suite is evidence for the exercised cases, not proof of every possible execution path.

Status definitions:

- **Enforced** — a dedicated architecture test fails when the stated contract changes.
- **Verified** — existing behavioral or provider tests exercise the behavior, but the architecture suite does not independently enforce the design decision.
- **Unverified** — no sufficiently deterministic automated check currently establishes the guarantee.

| Area | Contract or guarantee | Status | Enforcement / evidence |
|---|---|---|---|
| Package boundaries | Core packages do not depend on infrastructure adapter namespaces or projects/packages. | Enforced | ArchUnitNET type dependency rules and project/package reference inspection. |
| Runtime/test separation | Runtime projects do not reference test projects or test-helper packages. | Enforced | Project and package reference inspection. |
| Runtime source policy | Dynamic dispatch, activation, generic reflection, assembly scans, query-filter bypasses, and raw SQL usages are reported and require exact, reasoned exceptions. | Enforced | Roslyn syntax inspection plus shrinking allowlist. This is a source-policy guard, not a semantic proof that all equivalent APIs are detected. |
| Source generator compatibility | The mediator and mapping source generators target `netstandard2.0`. | Enforced | Project metadata contract test validates both generator projects. |
| Domain identity | Entity identity is exposed read-only and remains privately owned by the entity implementation. | Enforced | Reflection-based public contract tests. |
| Domain lifecycle | Entity lifecycle state remains encapsulated behind the framework's lifecycle methods. | Enforced | Reflection-based public contract tests. |
| Repository and factory shape | Repository and factory generic constraints remain tied to framework entities and their key types. | Enforced | Reflection-based generic constraint and method signature tests. |
| Aggregate and value-object shape | Aggregate event constraints, aggregate identity base type, and self-typed value-object equality remain stable. | Enforced | Reflection-based generic contract tests. |
| Dependency registration | Matching service/implementation pairs are idempotent; a repeated pair with a different lifetime fails rather than silently changing ownership. | Enforced | Direct behavioral tests of the registration strategy. |
| Dependency injection lifetimes | Marker-based scanning maps scoped and singleton markers to the declared lifetimes; scoped instances are reused only within a scope, singleton instances are shared across scopes, transient instances are distinct, and scoped/transient disposable instances are disposed with their owning scope. | Verified | Dependency-injection integration tests assert descriptor lifetimes, scope boundaries, and disposal using a scope-validating service provider. The architecture suite does not assert a universal lifetime policy for application services. |
| Mediator composition | Core mediator registration uses a scoped dispatcher and is safe to call repeatedly. | Enforced | Service descriptor contract test. |
| Mediator execution | Command, query, notification, event, and generated/reflection registration paths retain their tested dispatch behavior; cancellation reaches command handlers and concurrent dispatch exercises the shared invoker cache. | Verified | Existing mediator unit and integration tests, including explicit cancellation-propagation and 512-way concurrent-dispatch regression tests. |
| Mapper source generation | A partial mapper implementing `IMapper<TSource, TDestination>` receives a generated implementation that maps matching properties. | Verified | Consumer-level generator contract test compiles and executes the generated mapper; existing mapper tests cover configuration, concurrency, and disposal. |
| Rule governance | Source-policy rules have stable IDs, severity, and descriptions; every exception references the matching rule ID and exact path. | Enforced | Architecture tests validate catalogue uniqueness/completeness, exception ID alignment, reasons, unexpected findings, and stale allowlist entries. |
| Async cancellation contracts | Unit-of-work, mediator, message publishing/sending, listener, inbox, and outbox abstractions expose cancellation-aware asynchronous operations. | Enforced | Reflection-based API contract tests. This verifies API shape; propagation is exercised by behavioral tests. |
| Unit-of-work behavior | Transaction ownership is explicit; repeated or externally nested begin operations fail; failed commits, cancellation, rollback, and abandoned async scopes release owned transactions. | Verified | PostgreSQL integration tests cover commit, rollback, repeated begin, and disposal cleanup; mediator pipeline tests cover cancellation and configured rollback behavior. |
| Optimistic concurrency | Entities opting into `ConcurrencyTrackedEntity<TKey>` receive a provider-translated concurrency token that advances on tracked writes and set-based repository updates/deletes; stale writes fail rather than overwrite newer state. | Verified | Shared real-engine relational contract exercises stale-write rejection and bulk-update token advancement across PostgreSQL, MariaDB, MySQL, SQL Server, and Oracle. |
| Repository bulk delete | Bulk deletion is audited soft delete by default; physical deletion requires explicit opt-in. Tenant filters, affected-row counts, audit metadata, and concurrency-token advancement are preserved. | Verified | Shared real-engine relational contract exercises soft-delete and explicit hard-delete behavior across all five qualified relational providers. |
| Relational persistence | Tenant filters, repository behavior, bulk updates, ownership mutation rejection, and tenant isolation hold across PostgreSQL, SQL Server, Oracle, MySQL, and MariaDB. | Verified | The shared real-engine relational tenant-isolation contract runs in all five provider suites; provider-specific persistence and transaction tests add provider-specific evidence. |
| Non-relational persistence | Cosmos DB and MongoDB behavior remains correct for their separately tested paths. | Verified | Provider-specific tests. These providers are not covered by the relational qualification matrix. |
| Messaging delivery | Broker delivery, acknowledgement, retries, dead-letter handling, and shutdown behavior work for exercised Kafka and RabbitMQ paths. | Verified | Broker-backed and transport integration tests. |
| Message resource ownership | The transport sender abstraction remains disposable synchronously and asynchronously. | Enforced | Public interface contract test. |
| Tenant execution context | Execution context construction rejects empty tenant IDs and non-active tenants; its exposed context properties remain read-only. | Enforced | Constructor behavior and reflection contract tests. |
| Tenant lifecycle/isolation | HTTP and message execution contexts are initialized once, cleared, and isolated across the tested request/delivery lifecycles. | Verified | Existing tenant lifecycle integration tests. |
| Native AOT mapper path | The generated mapper consumer is published and executed as a trimmed Native AOT executable. | Enforced | CI publishes and executes a dedicated consumer using the prebuilt mapping runtime and generator assemblies, with linker warnings treated as errors. |
| AOT and trimming | Full Native AOT and trimmed compatibility across all runtime packages is not established. Mediator generated-provider discovery still uses reflective type lookup and an `Assembly.GetTypes`/`Activator` compatibility path. | Unverified | The mapper smoke gate qualifies only the generated mapping path. Mediator provider activation and other runtime packages still require dedicated trimmed and Native AOT consumer tests; the reflective fallback remains an explicit source-policy exception. |
| Public API compatibility | Published packages are checked against the previous stable package API. | Enforced | SDK package validation compares packages built from source against the matching 2.3.4 package baseline during package generation; intentional breaking changes must be explicitly reviewed and versioned.
| Security completeness | The source policy catches selected risky API families, but it is not a complete security analyzer and does not prove that arbitrary application/provider-specific paths are tenant-safe. | Unverified | Source-policy rule IDs and exact exceptions are catalogued; security and tenant-boundary tests cover defined paths only. Raw SQL and direct provider access remain explicit application responsibilities. |

## Governance rules

1. Every new blocking rule must name the framework guarantee it protects.
2. A rule must have a deterministic detection mechanism and an actionable failure message.
3. Behavioral rules require positive and negative cases where the contract permits them.
4. Exceptions must identify an exact rule and path, explain why the exception is necessary, and be removed when the finding is resolved.
5. Do not duplicate guarantees already enforced by the compiler or build system unless a distinct framework-level contract exists.
6. Do not promote an area from Verified to Enforced merely because its tests happen to run in the same CI workflow.
7. Every source-policy rule has a stable ID, severity, and description in `source-policy-rule-catalog.json`; allowlist exceptions must reference the matching ID.
8. Unverified items remain visible; they are not silently implied by a green build.
9. Framework-level rules belong here. Application-layer conventions belong in the separate Franz Architectural Tribunal in the template repository.

## Scope boundaries

These tests protect stable guarantees made by Franz.Common. They do not impose generic enterprise-application layering rules on framework packages, and they do not replace provider qualification, security review, application architecture tests, or production observability.

---

| HTTP JWT configuration | The symmetric JWT helper requires issuer, audience, and a signing key of at least 256 bits; issuer, audience, lifetime, expiration, signed-token, and signing-key validation cannot be silently disabled by missing configuration. | Enforced | `JwtBearerConfigurationTests` validates fail-fast configuration and all required `TokenValidationParameters`. |
| HTTP CORS boundary | The default CORS policy grants no cross-origin access without explicit origins; configured origins must be exact HTTP/HTTPS origins without wildcards or paths. | Enforced | `HttpBoundaryConfigurationTests` covers absent origins, exact origins, credentials, and rejected wildcard/invalid origins. |
| Forwarded-header trust | Forwarded-header processing preserves trusted proxy/network restrictions and allows explicit proxy configuration. | Enforced | `HttpBoundaryConfigurationTests` verifies the default trusted-proxy set remains present and configured proxies are retained. |
| HTTP identity boundary | User identity comes only from authenticated claims; tenant/domain identity comes only from the validated tenant execution context. | Enforced | `IdentityContextAccessorTests` verifies unauthenticated and conflicting request headers cannot establish identity and that validated tenant context is authoritative. |
| Tenant HTTP lifecycle | Request-aborted cancellation reaches resolution, validation, and membership authorization; validated tenant/domain context cannot be replaced and is cleared after downstream failure. | Verified | HTTP middleware, resolver, store, accessor, and lifecycle tests cover the exercised paths. I/O tenant stores must override cancellation-aware methods to cancel provider operations. |
| HTTP error responses | MVC error strategies control their response messages; technical errors are sanitized in production and business exceptions map to HTTP 422 in the global handler. | Verified | `HttpErrorContractTests` covers strategy-based sanitization, default fallback registration, and global business-error classification. |
