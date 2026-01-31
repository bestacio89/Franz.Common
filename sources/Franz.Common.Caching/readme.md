# Franz.Common.Caching

A full-featured caching module for the **Franz Framework**.
Provides **unified cache abstractions**, **hybrid caching (L1 + L2)**, Mediator request caching, settings cache, and **first-class observability** via Serilog and OpenTelemetry.

---

* **Current Version**: **1.7.7**
* **Target Framework**: **.NET 10.0**

---

## ✨ Features

* 🧱 **Unified abstractions**

  * `ICacheProvider`
  * `ICacheKeyStrategy`
  * `ISettingsCache`
  * **Single cache options model** (TTL, sliding, priority, tags)

* ⚡ **Hybrid caching support**

  * L1: In-memory (fast, local)
  * L2: Distributed / Redis (durable, shared)
  * Read-through + write-through behavior
  * Optional local cache hinting

* 🧩 **Pluggable providers**

  * `MemoryCacheProvider`
  * `DistributedCacheProvider`
  * `RedisCacheProvider`

* 🔑 **Flexible cache key strategies**

  * Default (type + normalized payload)
  * Namespaced (domain separation)
  * Custom strategies (drop-in)

* 🔁 **Mediator pipeline integration**

  * Automatic cache HIT / MISS detection
  * Transparent response caching
  * Per-request cache options (TTL, sliding, bypass)
  * CQRS-aware (queries cached, commands skipped)
  * **Null response caching supported**

* 📈 **Observability out of the box**

  * **Serilog enrichment**

    * `FranzCorrelationId`
    * `FranzCacheKey`
    * `FranzCacheHit`
  * **OpenTelemetry metrics**

    * cache hits / misses
    * lookup latency
  * **OpenTelemetry trace tags**

    * `franz.cache.*`

* 🔌 **Plug-and-play DI extensions**

  * `AddFranzMemoryCaching()`
  * `AddFranzDistributedCaching<T>()`
  * `AddFranzRedisCaching()`
  * `AddFranzHybridCaching()`
  * `AddFranzMediatorCaching()`

---

## 📦 Installation

```bash
dotnet add package Franz.Common.Caching
```

---

## 🚀 Quickstart

```csharp
// Program.cs
builder.Services.AddFranzHybridCaching();

builder.Services.AddFranzMediatorCaching(opts =>
{
    opts.DefaultTtl = TimeSpan.FromMinutes(10);
    opts.ShouldCache = req => !req.GetType().Name.EndsWith("Command");
});
```

---

## 🧠 Cache Options (Unified)

```csharp
public sealed class CacheOptions
{
    public TimeSpan? Expiration { get; init; }
    public TimeSpan? LocalCacheHint { get; init; }
    public bool Sliding { get; init; }
    public CachePriority Priority { get; init; } = CachePriority.Normal;
    public string[]? Tags { get; init; }
}
```

✔ Same options model used across **memory, distributed, redis, and mediator pipeline**
✔ Providers adapt internally — no provider-specific leakage

---

## 🧪 Example Mediator Query

```csharp
public record GetUserByIdQuery(int Id) : IQuery<User>;
```

* First execution → **MISS** → handler executes → cached
* Subsequent executions → **HIT** → cached response returned
* Logs + metrics + traces emitted automatically

---

## 🧰 Providers

### In-memory (L1)

```csharp
services.AddFranzMemoryCaching();
```

### Distributed (L2 – SQL Server, NCache, etc.)

```csharp
services.AddFranzDistributedCaching<SqlServerCache>();
```

### Redis (L2)

```csharp
// Simple
services.AddFranzRedisCaching("localhost:6379");

// Advanced (DI-driven)
services.AddFranzRedisCaching(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return ConnectionMultiplexer.Connect(
        cfg.GetConnectionString("Redis")
    );
});
```

### Hybrid (Recommended)

```csharp
services.AddFranzHybridCaching();
```

* Memory → Redis / Distributed
* Local cache warming
* Consistent invalidation flow

---

## 📊 Observability

### Serilog (example)

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

## 🧭 Roadmap

* Hashed / compact cache keys (Redis-friendly)
* Tag-based invalidation
* Deeper integration with `Franz.Common.Settings`
* Optional async background refresh

---

## 📝 Changelog

### **1.7.7**

* Unified cache options across all providers
* Hybrid cache provider stabilized (L1 + L2)
* Removed legacy entry options
* Provider internals aligned with Mediator pipeline
* Improved null caching semantics
* Documentation refresh (this file)

### **1.7.6**

* Lazy infrastructure loading
* Redis startup stability improvements

### **1.6.20**

* Upgraded to **.NET 10.0**

---

