# Franz.Common.Caching

> Provider-neutral caching abstractions and .NET 10 provider integrations, including memory, distributed, Redis, HybridCache, and Mediator query caching.

## Overview

Franz.Common.Caching provides a consistent cache contract, configurable providers,
tenant-aware key strategies, settings caching, and cache observability for .NET 10
applications.

## Installation

```bash
dotnet add package Franz.Common.Caching
```

## Provider contract

`ICacheProvider` exposes lookup and removal operations:

- `GetOrSetAsync<T>`: read an existing entry or create and cache the factory result.
- `RemoveAsync`: remove an entry.

Providers that support unconditional replacement also implement the separate
`ICacheWriter` capability. `SetAsync<T>` always replaces an entry and never
invokes a factory. Built-in providers implement both interfaces.

A JSON `null` is a value, not a cache miss. Distributed and Redis providers
treat malformed serialized entries as misses and replace them after the factory
succeeds.

Memory, distributed, and Redis providers coordinate concurrent factories with a
process-local keyed lock. HybridCache uses its own stampede protection. These
mechanisms do not provide a cross-process or cross-node distributed lock.

### Expiration behavior

| Provider | Absolute expiration | Sliding expiration |
| --- | --- | --- |
| Memory | Supported | Supported |
| `IDistributedCache` adapter | Supported | Supported by the underlying provider |
| Redis | Enforced as a maximum lifetime | Refreshed on access, never beyond the absolute deadline |
| HybridCache | Maps to HybridCache entry expiration | Maps to local-cache expiration; it is not a distributed sliding TTL |

A zero expiration disables that expiration dimension. Negative values and
lifetimes above 100 years are rejected during options validation. Per-call
options and metadata-provider TTL overrides are validated before a cache lookup
or factory invocation; invalid overrides fail fast rather than silently disabling
expiration. HybridCache
uses an explicit long-lived entry option for disabled expiration and caps its
L1 lifetime at the absolute expiration.

## Registration

### Memory cache

```csharp
services.AddFranzMemoryCaching(options =>
{
    options.KeyPrefix = "orders:";
    options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(30);
    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(5);
});
```

### Distributed cache adapter

Register an `IDistributedCache` implementation first, then add the Franz
adapter:

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
});
services.AddFranzDistributedCaching(configuration);
```

### Direct Redis provider

```csharp
services.AddFranzRedisCaching(options =>
{
    options.ConnectionString = configuration.GetConnectionString("Redis");
    options.KeyPrefix = "orders:";
});
```

The Redis provider uses StackExchange.Redis commands and Lua scripts for atomic
sliding-expiration refresh and absolute-expiration enforcement. It persists the
per-entry expiration policy, including an explicit no-expiration marker, so a
later read using different defaults cannot introduce an unintended TTL.
StackExchange.Redis does not expose cancellation-token overloads for these
commands: cancellation is observed before issuing a command and while waiting
for the local keyed lock or running the factory, but an already-issued Redis
command cannot be aborted by the provider.

### HybridCache

```csharp
services.AddFranzHybridCaching(options =>
{
    options.KeyPrefix = "orders:";
    options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(30);
    options.LocalCacheHint = TimeSpan.FromMinutes(2);
});
```

HybridCache uses its local cache by default. Register an `IDistributedCache`
implementation separately when a distributed L2 store is required. Its stampede
coordination is scoped to one `HybridCache` instance and does not provide a
cross-node lock. L2 read failures are treated as misses; L2 write failures are
logged but do not fail the caller, so HybridCache provides best-effort L2
semantics. Use the direct Redis or Distributed providers when backend failures
must propagate to the caller. Franz replaces
HybridCache's reflection-based default serializer factory with a metadata-only
factory. Register `AddFranzCachingJsonTypeInfoResolver` for every cached value
type before the provider is resolved; missing metadata fails explicitly. You may
register additional HybridCache serializers after `AddFranzHybridCaching` so
they take precedence for their supported types.

## Cache-key serialization and Native AOT

The default key strategy hashes the serialized request payload with SHA-256 and
includes the runtime request type in the key. The payload itself is not placed
in the cache key.

For trimming and Native AOT, register source-generated JSON metadata for every
request or cached value type used by the configured providers:

```csharp
[JsonSerializable(typeof(GetCustomerQuery))]
[JsonSerializable(typeof(CustomerDto))]
internal partial class ApplicationJsonContext : JsonSerializerContext
{
}

// Register before resolving the cache provider or key strategy.
services.AddFranzCachingJsonTypeInfoResolver(ApplicationJsonContext.Default);
services.AddFranzMemoryCaching();
```

The resolver is shared by the default key strategy, JSON-based Distributed/Redis
providers, and the HybridCache serializer factory. Native AOT applications fail
explicitly when required metadata is missing rather than relying on runtime
reflection.

The HybridCache integration uses Franz's metadata-only serializer, but the
current Microsoft.Extensions.Caching.Hybrid registration path also roots its
built-in reflection-based serializer factory. That dependency emits linker
warnings during strict Native AOT publishing even when Franz removes the
built-in factory from the runtime service collection. Consequently, the
warning-clean AOT smoke test covers Franz's key strategy, Distributed provider,
and Mediator caching pipeline; it deliberately does not claim that the current
HybridCache package integration is warning-clean under Native AOT. Keep the
HybridCache serializer metadata registered and validate that integration with
the regular HybridCache provider tests.

## Mediator integration

```csharp
services.AddFranzMediatorCaching(configuration);
```

The configuration section is `Mediator:Caching`. Only requests implementing
`IQuery<TResponse>` are eligible. Commands bypass the cache even if they also
implement a query interface. Registered metadata providers may veto caching or
supply a request-specific TTL.

## Tenant-scoped keys

Tenant isolation is opt-in:

```csharp
services.AddFranzMemoryCaching();
services.AddTenantScopedCacheKeys();
```

When this strategy is registered, a validated tenant execution context is
mandatory. Missing context throws; it never falls back to a shared global
namespace. Tenant-less services should not register the tenant-scoped strategy
and can use the default key strategy without requiring tenant infrastructure.

## Settings cache

`ISettingsCache` uses a 12-hour cache policy. `SetSettingAsync` performs an
explicit replacement write through `ICacheWriter`, including when a previous
read cached the default value for a missing setting. Custom providers used with
settings writes must implement `ICacheWriter`.

## Observability

Register observers after the provider and then decorate it:

```csharp
services.AddFranzMemoryCaching();
services.AddCacheObserver<LoggingMetricsObserver>();
services.AddCacheObserver<MetricsCacheObserver>();
services.AddObservableCaching();
```

Observer callbacks are synchronous because `ICacheObserver` is a synchronous
contract. Observer exceptions are isolated and written to `Trace`; they do not
change the outcome of the cache operation. No unbounded fire-and-forget task
queue is created. Per-key diagnostic tracking and tag history are bounded.

### Excel export

`ExcelCacheObserver` is included in the core package and uses EPPlus 8.
EPPlus 8 requires the consuming application to configure its license according
to its own licensing terms before exporting.

## Configuration

The cache options section is `Caching`:

```json
{
  "Caching": {
    "KeyPrefix": "orders:",
    "DefaultAbsoluteExpiration": "00:30:00",
    "DefaultSlidingExpiration": "00:05:00"
  },
  "Mediator": {
    "Caching": {
      "Enabled": true,
      "DefaultTtl": "00:05:00",
      "DefaultSlidingExpiration": "00:02:00"
    }
  }
}
```

The default key prefix is `franz:`. Redis connection strings should come from
a secret store or environment configuration. `CacheOptions.ToString()` redacts
the connection string. The legacy `EnableDistributedCache` flag does not select
a provider; provider selection is explicit through the registration methods.

## License

MIT. See the repository [LICENSE](../../LICENSE).
