# Caching benchmarks

This BenchmarkDotNet project measures Franz cache-provider operations independently of application and database work.

## Scenarios

| Benchmark | Measurement |
| --- | --- |
| Hit | Existing-entry lookup and provider read/deserialization |
| Miss | Factory execution and population of a new key |
| Write | Unconditional write through ICacheWriter |
| Overwrite | Replacement of an existing entry |
| Concurrent hits | Parallel reads across 1, 8, and 32 pre-populated keys |
| Key generation | Configured JSON metadata, SHA-256 digest and opaque key construction |

Providers: MemoryCache, DistributedCache backed by AddDistributedMemoryCache, HybridCache with in-process distributed L2, and Redis when FRANZ_CACHE_BENCHMARK_REDIS is set.

The in-process DistributedCache and HybridCache scenarios quantify adapter/serialization overhead. They do not represent networked Redis latency.

## Run

```bash
dotnet run --project benchmarks/Franz.Common.Caching.Benchmarks/Franz.Common.Caching.Benchmarks.csproj --configuration Release
```

Redis is opt-in. Set FRANZ_CACHE_BENCHMARK_REDIS to a dedicated Redis connection string before running.

## Methodology

- BenchmarkDotNet ShortRunJob is a CI smoke profile, not publication-grade statistical evidence.
- MemoryDiagnoser reports managed allocations and GC collection counts alongside latency/throughput summaries.
- Payload sizes are 128 B, 4 KiB and 64 KiB; concurrent-hit levels are 1, 8 and 32.
- Misses use unique keys; hits are pre-populated during global setup. Setup is excluded from measured operations.
- All providers receive identical payloads and cache policy.
- Redis measurements require a controlled dedicated instance. Record Redis version, topology, network location, persistence/configuration and host details with results.
- Compare providers only within the same run and machine. In-process DistributedCache numbers must not be presented as remote-cache latency.
- CI artifacts are measurements, not performance gates. No machine-dependent threshold is asserted until a stable reference environment and baseline exist.

For publication-grade results, use a quiet fixed-frequency host, document the SDK/runtime and Redis topology, increase iteration counts beyond the CI short run, and retain the full BenchmarkDotNet artifacts.
