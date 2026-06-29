Here’s an updated **README** for version **1.7.8**, including all the **observability improvements, metrics observer, logging + metrics observer, and Excel/export-ready observer** you’ve just implemented:

---

# Franz.Common.Caching

A full-featured caching module for the **Franz Framework**.
Provides **unified cache abstractions**, **hybrid caching (L1 + L2)**, Mediator request caching, settings cache, and **first-class observability** via Serilog, OpenTelemetry, and custom metrics/log observers.

---

* **Current Version:** v2.2.13
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

  * **Observers**

    * `MetricsCacheObserver` – in-memory metrics, hits, sets, total cache weight
    * `LoggingMetricsObserver` – combines Serilog logging + metrics
    * `ExcelCacheObserver` – export cache statistics (hits, misses, size) to Excel

---

## 🔌 Plug-and-play DI extensions

* `AddFranzMemoryCaching()`
* `AddFranzDistributedCaching<T>()`
* `AddFranzRedisCaching()`
* `AddFranzHybridCaching()`
* `AddFranzMediatorCaching()`
* `AddObservableCaching()` – **enables any combination of observers**
* `AddLoggingCacheObserver()`
* `AddMetricsCacheObserver()`
* `AddLoggingMetricsObserver()`
* `AddExcelCacheObserver()` – opt-in for Excel exports

> Observers are **opt-in**. You can register any combination depending on telemetry/logging needs.

---

## 📦 Installation

```bash
dotnet add package Franz.Common.Caching --version 1.7.8
```

---

## 🚀 Quickstart

```csharp
// Program.cs
builder.Services.AddFranzHybridCaching();

// Mediator caching
builder.Services.AddFranzMediatorCaching(opts =>
{
    opts.DefaultTtl = TimeSpan.FromMinutes(10);
    opts.ShouldCache = req => !req.GetType().Name.EndsWith("Command");
});

// Observers
builder.Services.AddObservableCaching()
    .AddLoggingCacheObserver()
    .AddMetricsCacheObserver()
    .AddLoggingMetricsObserver()
    .AddExcelCacheObserver(); // optional Excel export
```

---

## 📊 Observability Examples

### Metrics Observer

Tracks per-key stats:

* Hits / Misses
* Last access / set time
* Estimated size in bytes
* Total cache weight

```csharp
var snapshot = metricsObserver.Snapshot();
var totalWeight = snapshot.Values.Sum(x => x.EstimatedSizeBytes);
```

### Logging + Metrics Observer

Combines Serilog logs + OpenTelemetry metrics:

```json
{
  "FranzCorrelationId": "af42c6...",
  "FranzPipeline": "CachingPipeline",
  "FranzCacheKey": "GetUserById:{\"Id\":42}",
  "FranzCacheHit": true,
  "Message": "Cache HIT for GetUserByIdQuery in 0.6ms"
}
```

### Excel Observer

Exports relevant stats only (hits/misses/size) to Excel for **offline analysis**.

---

## 📝 Changelog
### V2.2.7 - Enhanced Configuration 
- Configuration section is now Caching instea "Franz:Caching" for clarity and consistency.

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

### **1.7.8**

* Added **MetricsCacheObserver** – in-memory hit/miss, per-key stats, total cache weight
* Added **LoggingMetricsObserver** – combines Serilog + metrics, tracks hits, sets, latency
* Added **ExcelCacheObserver** – export relevant cache stats to Excel
* Updated DI extensions – opt-in observer registration methods
* Exposed **CurrentKeys** property on observers for testability
* Observers integrated with **Redis, Distributed, Memory** caches
* Internal improvements for latency tracking in `CacheAccessDescriptor`

### **1.7.7**

* Unified cache options across all providers
* Hybrid cache provider stabilized (L1 + L2)
* Removed legacy entry options
* Provider internals aligned with Mediator pipeline
* Improved null caching semantics
* Documentation refresh

### **1.7.6**

* Lazy infrastructure loading
* Redis startup stability improvements

### **1.6.20**

* Upgraded to **.NET 10.0**

---

Perfect — we can make a **small ASCII/flow diagram** for the README that’s simple, readable, and shows the **cache → observers → outputs** flow. Here’s a suggestion you can drop right under the Observability section:

```
Cache Operation Flow
-------------------

        ┌───────────────┐
        │ ICacheProvider│
        └───────┬───────┘
                │ OnCacheSet / OnCacheHit / OnCacheRemove
                ▼
        ┌────────────────────┐
        │ ObservableCacheProvider │
        └───────┬────────────┘
                │
      ┌─────────┼─────────┐
      ▼         ▼         ▼
┌───────────┐ ┌──────────────┐ ┌───────────────┐
│ Logging   │ │ Metrics      │ │ Excel Export  │
│ Observer  │ │ Observer     │ │ Observer      │
└───────────┘ └──────────────┘ └───────────────┘

```


* `ICacheProvider` → executes cache operations (set, get, remove).
* `ObservableCacheProvider` → wraps the cache provider and broadcasts to **all registered observers**.
* Observers can be:

  * **LoggingCacheObserver** → Serilog / log enrichment
  * **MetricsCacheObserver** → in-memory stats, hits, misses, cache weight
  * **LoggingMetricsObserver** → combined logs + metrics
  * **ExcelCacheObserver** → exports relevant stats to Excel

This makes it **instantly clear** how any cache action flows through the system and where the data ends up.

If you want, I can **also make a slightly fancier Markdown/mermaid diagram** that will render nicely on GitHub and looks professional in the README. Do you want me to do that too?

