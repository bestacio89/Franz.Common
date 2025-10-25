# Franz.Common.Caching

A full-featured caching module for the** Franz Framework**.  
Provides pluggable cache providers(Memory, Distributed, Redis), request caching via Mediator pipelines, settings cache, and built-in observability with Serilog + OpenTelemetry.

---
- **Current Version**: 1.6.18
---

## ? Features

- ?? ** Unified abstractions**: `ICacheProvider`, `ICacheKeyStrategy`, `ISettingsCache`.
- ?? ** Multiple providers out of the box**:
  - `MemoryCacheProvider` (fast local)
  - `DistributedCacheProvider` (SQL Server, NCache, etc.)
  - `RedisCacheProvider` (scalable distributed caching).
- ?? ** Flexible key strategies**:
  - Default(type + serialized payload)
  - Namespaced(domain separation)
  - Custom(plug in your own).
- ?? ** Mediator pipeline**:
  - Automatic cache HIT/MISS detection
  - Transparent response caching
  - Per-request TTL + bypass
  - Consistent with CQRS(cache queries, skip commands).
- ?? ** Observability out of the box**:
  - Enriched Serilog logs(`FranzCorrelationId`, `FranzCacheKey`, etc.)
  - OpenTelemetry** metrics**: hits, misses, lookup latency
  - OpenTelemetry** trace tags**: `franz.cache.*`
- ?? ** Plug-and-play DI extensions**:
  - `AddFranzMemoryCaching()`
  - `AddFranzDistributedCaching<T>()`
  - `AddFranzRedisCaching()` (connection string or DI factory)
  - `AddFranzMediatorCaching()`.

---

## ?? Installation

```bash
dotnet add package Franz.Common.Caching
````

---

## ? Quickstart

```csharp
// Program.cs
builder.Services.AddFranzCaching(); // defaults to in-memory
builder.Services.AddFranzMediatorCaching(opts =>
{
    opts.DefaultTtl = TimeSpan.FromMinutes(10);
    opts.ShouldCache = req => !req.GetType().Name.EndsWith("Command");
});
```

### Example Mediator Query

```csharp
public record GetUserByIdQuery(int Id) : IQuery<User>;
```

*First execution ? MISS ? fetch + cache
* Subsequent executions with same input ? HIT ? return cached response

Logs and telemetry are produced automatically.

---

## ?? Providers

### In-memory

```csharp
services.AddFranzMemoryCaching();
```

### Distributed (SQL Server, NCache, etc.)

```csharp
services.AddFranzDistributedCaching<SqlServerCache>();
```

### Redis

```csharp
// Simple
services.AddFranzRedisCaching("localhost:6379");

// Advanced (DI-driven)
services.AddFranzRedisCaching(sp =>
{
  var cfg = sp.GetRequiredService<IConfiguration>();
  return ConnectionMultiplexer.Connect(cfg.GetConnectionString("Redis"));
});
```

---

## ?? Observability

### Serilog Logs

```json
{
  "FranzCorrelationId": "af42c6...",
  "FranzPipeline": "CachingPipeline",
  "FranzCacheKey": "GetUserById:{\"Id\":42}",
  "FranzCacheHit": true,
  "Message": "Cache HIT for GetUserByIdQuery in 0.6ms"
}
```

### OpenTelemetry Metrics

* `franz_cache_hits`
* `franz_cache_misses`
* `franz_cache_lookup_latency_ms`

### OpenTelemetry Trace Tags

* `franz.cache.key`
* `franz.cache.hit`
* `franz.cache.ttl_seconds`

---

## ?? Roadmap

* Support for **hashed cache keys** (short keys for Redis).
* More built-in strategies (e.g., sliding expiration).
* Deeper integration with `Franz.Common.Settings`.

---

# Franz.Common.Caching is production-ready:

? Cache providers + Mediator integration + observability, all out of the box.

