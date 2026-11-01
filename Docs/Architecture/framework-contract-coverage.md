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
| Source generator compatibility | The mediator source generator targets `netstandard2.0`. | Enforced | Project metadata contract test. |
| Domain identity | Entity identity is exposed read-only and remains privately owned by the entity implementation. | Enforced | Reflection-based public contract tests. |
| Domain lifecycle | Entity lifecycle state remains encapsulated behind the framework's lifecycle methods. | Enforced | Reflection-based public contract tests. |
| Repository and factory shape | Repository and factory generic constraints remain tied to framework entities and their key types. | Enforced | Reflection-based generic constraint and method signature tests. |
| Aggregate and value-object shape | Aggregate event constraints, aggregate identity base type, and self-typed value-object equality remain stable. | Enforced | Reflection-based generic contract tests. |
| Dependency registration | Matching service/implementation pairs are idempotent; a repeated pair with a different lifetime fails rather than silently changing ownership. | Enforced | Direct behavioral tests of the registration strategy. |
| Dependency injection lifetimes | Marker-based scanning and scoped/singleton/transient registration behavior remain correct for the exercised registration paths. | Verified | Existing dependency-injection integration tests. The architecture suite does not assert a universal lifetime policy for application services. |
| Mediator composition | Core mediator registration uses a scoped dispatcher and is safe to call repeatedly. | Enforced | Service descriptor contract test. |
| Mediator execution | Command, query, notification, event, and generated/reflection registration paths retain their tested dispatch behavior. | Verified | Existing mediator unit and integration tests. |
| Async cancellation contracts | Unit-of-work, mediator, message publishing/sending, listener, inbox, and outbox abstractions expose cancellation-aware asynchronous operations. | Enforced | Reflection-based API contract tests. This verifies API shape; propagation is exercised by behavioral tests. |
| Unit-of-work behavior | Transaction begin, commit, rollback, and persistence behavior work for the exercised EF Core paths. | Verified | Existing persistence and transaction tests. |
| Relational persistence | Tenant filters, repository behavior, bulk updates, transactions, and provider compatibility hold for the tested relational providers. | Verified | Provider-specific integration tests and the relational provider contract. |
| Non-relational persistence | Cosmos DB and MongoDB behavior remains correct for their separately tested paths. | Verified | Provider-specific tests. These providers are not covered by the relational qualification matrix. |
| Messaging delivery | Broker delivery, acknowledgement, retries, dead-letter handling, and shutdown behavior work for exercised Kafka and RabbitMQ paths. | Verified | Broker-backed and transport integration tests. |
| Message resource ownership | The transport sender abstraction remains disposable synchronously and asynchronously. | Enforced | Public interface contract test. |
| Tenant execution context | Execution context construction rejects empty tenant IDs and non-active tenants; its exposed context properties remain read-only. | Enforced | Constructor behavior and reflection contract tests. |
| Tenant lifecycle/isolation | HTTP and message execution contexts are initialized once, cleared, and isolated across the tested request/delivery lifecycles. | Verified | Existing tenant lifecycle integration tests. |
| AOT and trimming | Source-generator target compatibility is enforced, but full Native AOT and trimmed publish behavior is not established by these architecture tests. | Unverified | Requires dedicated publish/sample applications and runtime validation. |
| Public API compatibility | No automated baseline currently proves that every published package remains binary/source compatible. | Unverified | Requires an explicit public API baseline and compatibility gate. |
| Security completeness | The source policy catches selected risky API families, but it is not a complete security analyzer and does not prove that arbitrary application/provider-specific paths are tenant-safe. | Unverified | Security and tenant-boundary tests cover defined paths only; raw SQL and direct provider access remain explicit application responsibilities. |

## Governance rules

1. Every new blocking rule must name the framework guarantee it protects.
2. A rule must have a deterministic detection mechanism and an actionable failure message.
3. Behavioral rules require positive and negative cases where the contract permits them.
4. Exceptions must identify an exact rule and path, explain why the exception is necessary, and be removed when the finding is resolved.
5. Do not duplicate guarantees already enforced by the compiler or build system unless a distinct framework-level contract exists.
6. Do not promote an area from Verified to Enforced merely because its tests happen to run in the same CI workflow.
7. Unverified items remain visible; they are not silently implied by a green build.
8. Framework-level rules belong here. Application-layer conventions belong in the separate Franz Architectural Tribunal in the template repository.

## Scope boundaries

These tests protect stable guarantees made by Franz.Common. They do not impose generic enterprise-application layering rules on framework packages, and they do not replace provider qualification, security review, application architecture tests, or production observability.
