# Franz.Common.Caching

Provider-neutral caching for Franz.Common and .NET 10 applications. The package
contains memory, `IDistributedCache`, Redis, and .NET HybridCache adapters,
Mediator query caching, settings caching, tenant-aware key strategies, and
provider observability.

## Installation

```bash
dotnet add package Franz.Common.Caching
```

Excel export is optional and is distributed separately:

```bash
dotnet add package Franz.Common.Caching.Excel
```

## Provider contract

`ICacheProvider` exposes three operations:

- `GetOrSetAsync<T>`: read an existing entry or create and cache the factory result.
- `SetAsync<T>`: unconditionally replace an entry; the factory is never invoked.
- `RemoveAsync`: remove an entry.

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

A zero expiration disables that expiration dimension. Negative values are
rejected during options validation.

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
sliding-expiration refresh and absolute-expiration enforcement. StackExchange.Redis
does not expose cancellation-token overloads for these commands: cancellation
is observed before issuing a command and while waiting for the local keyed lock
or running the factory, but an already-issued Redis command cannot be aborted by
the provider.

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
implementation separately when a distributed L2 store is required.

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

The resolver is shared by the default key strategy and the JSON-based
Distributed/Redis providers. Missing metadata fails explicitly instead of
silently falling back to runtime reflection.

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
explicit replacement write, including when a previous read cached the default
value for a missing setting.

## Observability

Register observers after the provider and then decorate it:

```csharp
services.AddFranzMemoryCaching();
services.AddCacheObserver<LoggingCacheObserver>();
services.AddCacheObserver<MetricsCacheObserver>();
services.AddObservableCaching();
```

Observer callbacks are synchronous because `ICacheObserver` is a synchronous
contract. Observer exceptions are isolated and written to `Trace`; they do not
change the outcome of the cache operation. No unbounded fire-and-forget task
queue is created.

### Optional Excel export

Install `Franz.Common.Caching.Excel` to use `ExcelCacheObserver`. The optional
package owns the EPPlus dependency; the core caching package does not. EPPlus 8
requires the consuming application to configure its license according to its
own licensing terms before exporting.

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
the connection string.

## License

MIT. See the repository [LICENSE](../../LICENSE).
