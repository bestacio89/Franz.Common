# Caching production workload simulation

This executable is a self-contained .NET workload generator for Franz.Common Caching. It uses the BCL for virtual-user scheduling, timing, latency sampling and report generation. There is no third-party load-testing framework or separate license dependency.

## Providers

- memory: Franz MemoryCache provider.
- distributed: Franz DistributedCache provider backed by a real Redis instance through Microsoft.Extensions.Caching.StackExchangeRedis.
- hybrid: Franz HybridCache provider with Redis as its distributed L2 store.
- redis: Franz Redis provider using StackExchange.Redis directly.

Distributed, Hybrid, and Redis runs provision an isolated Redis 7.2 container with Testcontainers.Redis 4.16.0. This uses the upstream patched container dependency graph rather than carrying a direct SSH.NET security override. No external Redis service or developer-managed compose stack is required.

## Workload model

Each worker is a persistent virtual user. The harness staggers worker startup for ramp-up, holds the configured concurrency during the sustained phase, and progressively retires workers during ramp-down.

- Multi-tenant keyspace generated through the actual TenantScopedCacheKeyStrategy.
- Default operation mix: 80% reads, 15% unconditional writes, 5% overwrites.
- Skewed hot-key access distribution to model non-uniform application traffic.
- Mixed payload sizes: 128 B (50%), 4 KiB (40%), 64 KiB (10%).
- Partial warm-up of the keyspace and a separate measured warm-up phase.
- Configurable per-user think time to avoid modeling only an unbounded tight loop.
- Per-read tenant and key identity verification.
- Pre-flight assertions for tenant isolation, concurrent same-key factory execution, and absolute expiration.
- Per-operation throughput, mean latency, sampled p50/p95/p99 latency and maximum latency.
- Process CPU time, managed allocations and GC counts in the JSON summary.

Latency percentiles are calculated from a bounded deterministic sample (every 100th operation) to limit measurement overhead; operation counts, total latency and maximum latency are tracked across every operation. This makes the percentile estimates useful for regression comparisons without retaining millions of latency values in memory.

## Profiles

| Profile | Tenants | Keys / tenant | Prewarmed | Virtual users | Think time | Sustained phase |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ci | 32 | 250 | 30% | 24 | 5–10 ms | 45 seconds |
| full | 100 | 1,000 | 30% | 100 | 5–10 ms | 10 minutes |
| stress | 100 | 1,000 | 30% | 256 | 2–4 ms | 20 minutes |

The CI profile is a bounded regression signal, not a capacity certification. Nightly and weekly profiles provide longer observation windows. Workload parameters are explicit and versioned.

## Run locally

```bash
FRANZ_CACHE_LOAD_PROVIDER=hybrid FRANZ_CACHE_LOAD_PROFILE=ci \
  dotnet run --project benchmarks/Franz.Common.Caching.LoadTests/Franz.Common.Caching.LoadTests.csproj \
  --configuration Release -p:EnablePackageValidation=false
```

Supported provider values: memory, distributed, hybrid, redis. Supported profiles: ci, full, stress.

The package-validation override applies only to this harness build. Normal Release validation continues to enforce the published package compatibility baseline. Caching 3.0.0 intentionally changes the public API; the workload runner is not the package-compatibility gate.

## Reports and interpretation

Each provider writes load-report.html, load-report.md, load-report.csv and load-summary.json under reports/<provider>/. Reports include workload profile, commit SHA, runtime, OS, processor count, hit ratio, operation counts, latency distributions and process resource measurements.

Compare providers only on equivalent runner classes and within the same profile. GitHub-hosted runner measurements detect large regressions but are not stable enough for tight absolute latency gates. Introduce thresholds only after a dedicated, controlled runner establishes a statistically defensible baseline.

The harness fails on workload exceptions or correctness mismatches. It does not claim that in-process MemoryCache and remote Redis have equivalent failure or latency characteristics.
