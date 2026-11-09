# 📈 **Franz Framework – Full Changelog**

## v3.0.0 — Major platform release (planned)

### gRPC modernization
- Replace the legacy broad `Grpc` dependency with the supported `Grpc.Core.Api` contract package in `Franz.Common.Grpc`.
- Remove `Grpc.Net.Client` and custom channel ownership from the core package; use official `Grpc.Net.ClientFactory` typed-client registration and lifecycle management.
- Remove `IFranzGrpcClientFactory`, `FranzGrpcClientFactory`, and inert configuration switches; migrate through `AddGrpcClient<T>`.
- Apply the Franz execution pipeline at the ASP.NET Core transport boundary for unary, server-streaming, client-streaming, and duplex RPCs.
- Preserve per-message server-stream and duplex `WriteOptions` through the bounded transport bridge.
- Add complete streaming lifecycle logging and metrics, plus standard .NET ActivitySource and Meter instrumentation with bounded metric dimensions.
- Reduce public configuration to implemented tenant enforcement and unary client deadline settings, with startup validation.
- Keep the Franz client wrapper unary-only; use generated typed-client methods directly for outgoing streaming calls.
- Document 2.3.3-to-3.0.0 migration dispositions and retain the compiled API diff and package-consumer validation as release gates.

### Caching and performance validation
- Introduce provider-neutral cache contracts with MemoryCache, DistributedCache, Redis and HybridCache implementations.
- Enforce deterministic SHA-256 cache keys, explicit JSON metadata, tenant-scoped isolation and process-local same-key single-flight coordination.
- Define provider-specific expiration, cancellation, malformed-entry recovery, null-hit and failure semantics.
- Centralize cache metrics and observer behavior; bound diagnostic state and isolate observer failures.
- Add Native AOT smoke coverage for Franz-owned caching paths while documenting upstream HybridCache limitations.
- Add BenchmarkDotNet microbenchmarks and a BCL production-style workload runner covering all supported cache providers, multi-tenant keyspaces, hot-key skew, mixed payload sizes, warm-up, ramp-up, sustained load and ramp-down.
- Publish machine-readable and human-readable CI reports; keep hosted-runner performance thresholds disabled until controlled baselines exist.

### Release metadata
- Set all framework NuGet packages to version 3.0.0, with assembly and file versions aligned to 3.0.0.0.
- Keep package API compatibility validation anchored to the last published stable baseline (2.3.4); this baseline is deliberately distinct from the new package version.
- Package publication remains gated on full Release validation and explicit release approval.

---

## v2.3.5 — Tenant-aware HTTP, document persistence and gRPC

### HTTP request lifecycle
- Compose `UseFranzMultiTenancy()` automatically inside `UseHttpArchitecture()`.
- Resolve tenants after authentication and before endpoint authorization.
- Place forwarded-header processing before routing and authentication, with exception handling at the start of the pipeline.

### Native document persistence
- Add a MongoDB tenant document contract and tenant-scoped repository with mandatory validated tenant filters, insert stamping, cross-tenant ownership rejection, batch insert validation, and a deployment-time compound tenant/document identity index.
- Add tenant-safe MongoDB atomic updates, bulk writes, immutable tenant ownership update builders, optional client sessions, and replica-set/sharded transaction execution.
- Scope MongoDB event-sourced aggregate replay, persistence and deletion by validated tenant identity; keep non-tenant event streams isolated with a null tenant key.
- Apply the shared tenant execution context and fail-closed query/write boundary to EF Core Cosmos contexts.
- Partition Cosmos tenant-owned entities by tenant identity, converting the Guid tenant identifier to the Cosmos-supported string partition key. Support a hierarchical tenant-first key when an entity also declares a custom partition component.
- Add a native Cosmos SDK tenant repository that supplies the validated tenant partition key to point reads, queries, create/replace/delete and read-many operations.
- Expose Cosmos RU charges, activity IDs, ETags and query continuation metadata; require ETags for native replacement to enforce optimistic concurrency.
- Keep MongoDB and Cosmos provider behavior native; do not claim relational EF bulk-update, transaction or query-filter guarantees for these stores.

### gRPC execution boundary
- Remove caller-supplied tenant and user metadata before server-side tenant resolution; reuse an already validated HTTP request tenant context without replacing or prematurely clearing it.
- Require valid Guid tenant identity, application membership authorization, optional shared tenant lifecycle validation, and scoped tenant execution-context initialization/cleanup.
- Strict `RequireTenant` configuration denies unresolved or unauthorized tenants.
- Propagate client tenant metadata only from a validated execution context.
- Scope server/client behavior providers to prevent scoped behavior instances from being cached across requests.

### Public API boundary corrections
- Split the MassTransit Kafka producer and consumer contracts; remove the combined `IMessagingClient`, the producer's unsupported subscribe stub, the consumer's unsupported publish stub, and the unscoped captured-handler overload. Kafka consumer handlers now always receive the per-delivery service provider.
- Move `TestDispatcher` out of the production Mediator package into `Franz.Common.Testing`.
- Remove the unsupported `ICacheProvider.RemoveByTagAsync` contract and its provider stubs; key-based invalidation is the supported cache API.
- Remove the `FranzSagaOptions.EnableValidation` alias; use `ValidateMappings`.
- Make `ITenantValidator.IsValidAsync(Guid, CancellationToken)` the sole validation contract; implementations must accept and honor cancellation.
- Remove redundant `GetCurrentId`/`SetCurrentId` aliases from messaging tenant/domain accessors and the synchronous `KafkaModel.Dispose()` entry point; the model is asynchronously disposable.

### Release boundary
- No changes to relational EF provider behavior or database routing. Messaging and cache API boundary corrections are listed above; they remove obsolete contracts without changing the underlying transport or cache execution strategies.
- Set the shared package version to 2.3.5 and file version to 2.3.5.0; retain assembly version 2.3.0.0.
- NuGet publication remains gated on successful Release build, full tests, and release checks.

---

## v2.3.4 — Tenant isolation and messaging execution boundaries
