# Franz.Common.Mediator upgrade roadmap

**Baseline:** .NET 10, Franz.Common 2.3.2  
**Compatibility rule:** preserve existing public APIs and V1 reflection-registration behavior. Keep the current package version while these changes are validated; do not bump versions or change downstream applications as part of this work.

## Delivery principles

- Measure before optimizing; benchmark results must identify runtime, configuration, workload, and allocation data.
- Keep the existing reflection-based registration API available for compatibility.
- Make generated registration deterministic and scoped to the explicitly selected handler assembly.
- Do not silently swallow registration failures or silently register handlers from unrelated assemblies.
- Keep commands, queries, notifications, events, and stream queries semantically distinct.
- Deliver in small pull requests with focused tests and CI evidence.

## Sequence

### 1. Correctness and baseline

- Cover V1 and generated registration, handler lifetimes, duplicate registrations, pipeline ordering, and dispatch behavior.
- Add a repeatable benchmark baseline for registration/startup and dispatch throughput/allocations.
- Record results before claiming performance improvements.

### 2. Deterministic registration and cache correctness

- Resolve generated registration from the requested application assembly.
- Remove cross-assembly discovery and load-order-dependent behavior.
- Preserve the explicit reflection fallback only within the requested assembly.
- Verify cache keys, thread safety, and stable behavior under concurrent dispatch.

**Current increment:** generated-registration lookup is restricted to the target assembly, and a regression test guards against handler registrations leaking from unrelated loaded assemblies.

### 3. Source-generator correctness

- Make generated output ordering stable and duplicate handling explicit.
- Validate supported handler shapes and emit actionable diagnostics for unsupported or inaccessible handlers.
- Test inherited interfaces, partial classes, multiple handler contracts, and generic-handler exclusions.
- Ensure generator failures are visible at build time.

### 4. Dispatch hot path

- Profile command/query/event dispatch and pipeline execution using the baseline.
- Optimize only measured hot paths; retain cached invokers where they improve results without changing exception, cancellation, scope, or lifetime semantics.
- Add allocation and concurrency regression checks where they are stable enough for CI.

### 5. Pipeline and ecosystem validation

- Verify validation, logging, auditing, telemetry, resilience, and transaction pipeline ordering and registration idempotency.
- Run integration tests through the existing Franz.Common mediator extensions and representative consumer composition.
- Keep changes within Mediator unless a cross-package contract change is demonstrably required.

### 6. Trimming and Native AOT validation

- Add a minimal consumer smoke-test for generated registration with trimming enabled and Native AOT where supported by CI.
- Treat reflection-based V1 as a compatibility path with its own documented constraints.
- Do not describe the generated path as AOT-safe until publish and execution checks pass.

## Exit criteria

- Full solution restore, Release build, and tests pass.
- Mediator unit and integration tests pass for both registration models.
- Benchmark comparisons are reproducible and show no unexplained regression.
- Generated registration is deterministic and cannot pick up handlers from unrelated assemblies.
- Any compatibility limitation and AOT result is documented before release.
