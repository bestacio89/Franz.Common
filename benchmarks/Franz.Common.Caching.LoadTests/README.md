# Caching production workload simulation

This executable is the repeatable production-style workload harness for Franz.Common Caching. It exercises the real provider registrations and public cache contracts; it is separate from isolated BenchmarkDotNet microbenchmarks.

## Providers

- memory: Franz MemoryCache provider.
- distributed: Franz DistributedCache provider backed by a real Redis instance through Microsoft.Extensions.Caching.StackExchangeRedis.
- hybrid: Franz HybridCache provider with Redis as its distributed L2 store.
- redis: Franz Redis provider using StackExchange.Redis directly.

Distributed, Hybrid, and Redis runs provision an isolated Redis 7.2 container with Testcontainers. No external Redis service or developer-managed compose stack is required.

## Workload model

- Multi-tenant keyspace generated through the actual TenantScopedCacheKeyStrategy.
- Default operation mix: 80% reads, 15% unconditional writes, 5% overwrites.
- Skewed hot-key access distribution to model non-uniform application traffic.
- Mixed payload sizes: 128 B (50%), 4 KiB (40%), 64 KiB (10%).
- Partial warm-up of the keyspace to avoid an unrealistically all-hot or all-cold cache.
- Per-read tenant and key identity verification.
- Pre-flight assertions for tenant isolation, concurrent same-key factory execution, and absolute expiration.
- NBomber per-operation latency distributions and throughput; process CPU time, managed allocations and GC counts are emitted in summary JSON.

## Profiles

| Profile | Tenants | Keys / tenant | Prewarmed | Virtual users | Sustained phase |
| --- | ---: | ---: | ---: | ---: | ---: |
| ci | 32 | 250 | 30% | 24 | 45 seconds |
| full | 100 | 1,000 | 30% | 100 | 10 minutes |
| stress | 100 | 1,000 | 30% | 256 | 20 minutes |

The CI profile is a bounded regression signal, not a capacity certification. Nightly and weekly profiles provide longer observation windows. Workload parameters are explicit and versioned.

## Run locally

```bash
FRANZ_CACHE_LOAD_PROVIDER=hybrid FRANZ_CACHE_LOAD_PROFILE=ci \
  dotnet run --project benchmarks/Franz.Common.Caching.LoadTests/Franz.Common.Caching.LoadTests.csproj \
  --configuration Release -p:EnablePackageValidation=false
```

Supported provider values: memory, distributed, hybrid, redis. Supported profiles: ci, full, stress.

The package-validation override applies only to this harness build. Normal Release validation continues to enforce the published package compatibility baseline. Caching 3.0.0 intentionally changes the public API; the load runner is not the package-compatibility gate.

## Reports and interpretation

Each provider writes NBomber HTML, Markdown, and CSV reports plus load-summary.json under reports/<provider>/. Reports include workload profile, commit SHA, runtime, OS and processor count.

Compare providers only on equivalent runner classes and within the same profile. GitHub-hosted runner measurements detect large regressions but are not stable enough for tight absolute latency gates. Introduce thresholds only after a dedicated, controlled runner establishes a statistically defensible baseline.

The harness fails on workload exceptions or correctness mismatches. It does not claim that in-process MemoryCache and remote Redis have equivalent failure or latency characteristics.