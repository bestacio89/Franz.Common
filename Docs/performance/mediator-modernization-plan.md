# Franz.Common.Mediator — Performance and Compatibility Modernization Plan

## Goals and invariants

- Preserve the public command, query, notification, event, handler, pipeline, observer, and registration APIs unless a versioned change is explicitly approved.
- Keep V1 reflection registration available as a deliberate compatibility mode.
- Make V2 source-generated registration deterministic and trim/AOT-safe, without scanning unrelated loaded assemblies.
- Preserve scoped handler dependencies and DI lifetime semantics.
- Keep pipeline ordering, exception propagation, cancellation, notification publish strategies, observer behavior, and correlation semantics covered by tests.
- Optimize measured hot paths; do not trade correctness, diagnostics, or maintainability for speculative micro-optimizations.

## Phase 0 — Establish a reproducible baseline

1. Pin SDK/toolchain and restore sources for CI; record commit SHA and package graph.
2. Run Release build and all mediator tests, then the complete solution suite.
3. Capture compiler/analyzer warnings and test failures as a baseline.
4. Add a dedicated BenchmarkDotNet project that is not part of normal unit-test timing.
5. Benchmark cold startup/registration and warmed dispatch separately. Cover command with/without response, query, notification with 0/1/many handlers, pipeline counts 0/1/3/6, V1 reflection registration, V2 generated registration, and concurrent dispatch.
6. Record throughput, mean and distribution (where available), allocated bytes/op, Gen0 collections, startup time, and generated assembly size. Use stable hardware/runtime settings and compare the same workload.

## Phase 1 — Correctness and compatibility characterization

Create explicit tests for:

- Request-to-handler matching and response type contracts, including cache-key collisions.
- Missing, duplicate, open-generic, abstract, inaccessible, and malformed handler registrations.
- V1 and V2 registration parity for supported handler shapes.
- Generated registration with no generated provider, multiple providers, and explicit target assemblies.
- DI lifetimes, scoped services, disposal, and concurrent resolution.
- Pipeline ordering, pre/post-processor ordering, and exact-once execution.
- Cancellation before dispatch, during handler execution, and during pipeline execution.
- Exception behavior for commands, queries, notifications, and observer failures.
- Sequential and parallel notification strategies, stop-on-first-failure vs continue-on-error, and aggregate failures.
- Async streams: enumeration, early disposal, cancellation, and exception propagation.
- Correlation/context isolation across concurrent async flows and nested dispatch.
- AOT/trimming smoke tests for generated registration.

Before changing any behavior, document the intended contract and assert it in tests. In particular, do not silently alter notification exception swallowing or observer failure semantics as part of a performance patch.

## Phase 2 — Registration architecture

1. Treat generated registration as an explicit compile-time artifact and register it directly through a generated, strongly typed entry point.
2. Remove the global loaded-assembly scan from V2. Missing generated registration should produce an actionable diagnostic or an explicitly selected compatibility fallback.
3. Keep reflection discovery confined to V1 (or a clearly named opt-in fallback) and make reflection failures observable.
4. Validate duplicate handler mappings and unsupported handler shapes deterministically.
5. Check generator target framework, Roslyn package compatibility, analyzer packaging, generated code nullable annotations, deterministic output, and diagnostic IDs.
6. Add generator unit tests using Roslyn compilation fixtures and integration tests that compile a consumer project and inspect registrations.
7. Verify generator package transitivity and behavior from a separate consumer project, not only from the mediator test project.

## Phase 3 — Dispatch hot-path design

Evaluate the following in isolation, retaining only improvements supported by benchmarks:

1. **Typed invoker cache:** key invokers by all runtime type dimensions that affect the compiled delegate (at minimum request and response types). Prefer strongly typed delegates where possible and validate handler compatibility during registration.
2. **Generated dispatch tables:** allow V2 to emit direct typed dispatch delegates, bypassing expression compilation and reflective invocation at runtime. Keep V1's current semantics.
3. **Pipeline composition:** resolve pipeline components once per DI scope/dispatch contract where safe; avoid global caching of scoped service instances. Consider building a reusable invocation plan per closed request/response contract.
4. **Processor execution:** replace repeated reflective processor discovery with generated or cached typed invocation plans. Avoid caching delegates that accidentally capture a scoped provider or request instance.
5. **Allocation control:** measure closures, async state machines, temporary arrays/lists, LINQ, boxing, and result casts. Remove allocations only where the resulting code remains clear and correct.
6. **Time measurement:** consider monotonic timestamps (`TimeProvider.GetTimestamp` / elapsed-time APIs) for duration metrics, keeping externally observable units stable.
7. **Observability overhead:** preserve observer APIs; measure no-op and active observer costs separately. Avoid forcing allocations when no observers are registered if this can be achieved without complicating dispatch semantics.
8. **Notifications:** benchmark sequential and parallel paths, including cancellation and error aggregation. Do not use fire-and-forget execution or unbounded task fan-out.
9. **Concurrency:** verify static caches are race-safe, bounded by actual closed message types, and do not cache service-provider instances or mutable scoped state.
10. **AOT/trimming:** annotate reflection-only paths accurately, test generated mode with Native AOT where practical, and keep linker warnings visible.

## Phase 4 — Benchmark and acceptance gates

- Baseline and optimized implementations run against the same benchmark inputs and runtime.
- No correctness, architecture, or compatibility test regressions.
- Generated registration is deterministic and does not scan all loaded assemblies.
- V2's generated path has no avoidable runtime handler discovery or expression compilation.
- Public API changes are zero unless explicitly approved.
- Report dispatch latency and allocation deltas for each scenario, including cases where performance does not improve.
- Require a meaningful improvement in at least one targeted workload without material regression in others before accepting a performance optimization.

## Phase 5 — Packaging and downstream compatibility

1. Pack the mediator and related optional pipeline packages.
2. Compile a consumer fixture using V1 and another using V2; include realistic handler, validator, observer, pipeline, and scoped-service registrations.
3. Run tests against Franz.Common.Mediator.Polly and Franz.Common.Mediator.OpenTelemetry to verify their pipeline contracts.
4. Identify repository consumers through GitHub code search and existing examples. Do not commit changes to downstream repositories without approval.
5. If an existing contract must change, provide migration guidance, obsolete APIs before removal where feasible, and use a major version when SemVer requires it.

## Phase 6 — Rollout

- Deliver changes as small PRs: (A) characterization tests/benchmarks, (B) registration correctness, (C) generated dispatch, (D) pipeline/processor optimization, (E) package and docs cleanup.
- Each PR contains tests, benchmark results when performance-related, documentation, and a rollback path.
- Keep V1 operational until real consumer projects pass compatibility validation.
- Publish no performance claims without reproducible results.
