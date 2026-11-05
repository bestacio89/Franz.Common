# Multitenancy execution lifecycle and persistence boundaries

## Execution-context contract

A `TenantExecutionContext` is immutable validated identity. Tenant resolution is not authorization: the HTTP and messaging adapters validate tenant status and authorization before establishing the context.

### HTTP requests

- Register `ITenantExecutionContextAccessor` as scoped through `AddFranzMultiTenancy`.
- `TenantExecutionContextAccessor` stores the validated context in the current `HttpContext.Items`; it does not use process-wide or static mutable state.
- `TenantResolutionMiddleware` initializes the context only after resolution, active-status checks, optional tenant validation, and authenticated-user authorization.
- The middleware clears execution, tenant, and domain entries in a `finally` block after downstream execution.
- A second initialization in the same request is rejected. This detects incorrect middleware composition instead of silently replacing identity.

### Messages and background consumers

- Register messaging tenancy services with `AddMessagingMultitenancyContext`; the execution accessor is scoped.
- The producer propagates tenant metadata in message headers. Headers are untrusted transport input, not proof of tenant membership.
- `TenantMessageContextInitializer.BeginAsync` resolves the tenant from the store, checks active status, applies the configured validator when present, authorizes the producer, validates any supplied domain against the tenant, and only then initializes execution context.
- The returned `IAsyncDisposable` owns the message execution lifetime. The Kafka, RabbitMQ, generic messaging hosted-service, Azure Service Bus, Azure Event Hubs and Outbox listener execution paths now invoke the initializer automatically after establishing the message context and before selecting or executing a handler. The returned scope is disposed after the complete handler pipeline on success, failure, and cancellation.
- The ambient message context is cleared in a `finally` block after tenant-scope disposal, including when tenant validation or handler execution fails. The Outbox listener also creates a fresh DI scope per stored message and resolves its stores, serializer and dispatcher from that scope.
- The initializer also accepts an explicit `Message`, allowing transport processors that do not use the ambient message accessor to establish the same validated tenant context.
- A message scope rejects a second initialization. A consumer must not reuse an initialized accessor for another message; create a new DI scope per message.
- The MassTransit Kafka adapter exposes only a scoped-handler contract that receives the per-delivery `IServiceProvider`; it maps Kafka headers into the tenant message envelope and invokes the same initializer. The unscoped `Func<T, Task>` overload has been removed, so handler dependencies cannot be captured outside the delivery scope.
- Background jobs must carry explicit tenant identity in authenticated job/message metadata and re-enter through the same validation and authorization boundary. Do not capture an HTTP accessor, copy a mutable accessor between jobs, or trust a tenant identifier supplied only in an arbitrary job payload.

## Persistence guarantees and limitations

### Tracked EF operations

`DbContextBase` applies tenant query filters to tenant-owned entities, stamps new entities with the current tenant, and validates tenant ownership on tracked updates/deletes. Tenant identity is also configured as a concurrency token to protect detached update paths.

### EF Core set-based bulk updates

`IEntityFrameworkBulkUpdateRepository<TEntity>` uses `ExecuteUpdateAsync` and composes the supplied predicate over the normal DbSet, retaining configured global query filters. The EF-specific contract is intentionally separate from provider-neutral business repository interfaces.

Bulk updates bypass materialization, change tracking, `SaveChanges`, save interceptors, audit hooks, and domain-event dispatch. Callers must explicitly set required audit columns and reload or clear tracked instances when consistency requires it. Tenant ownership reassignment is rejected by the repository implementation.

Support depends on provider translation. Relational providers that support EF Core's `ExecuteUpdateAsync` can execute supported expressions; non-relational providers such as Cosmos may reject the operation. Provider support must be tested against the exact provider/version and expression shapes used by an application.

### Raw SQL

Global query filters do not rewrite arbitrary SQL. The framework cannot claim tenant isolation for raw SQL that targets tenant-owned tables unless the database itself enforces it or the SQL access path applies a trusted tenant predicate.

For shared-table tenancy, use database row-level security where available, bind tenant identity from a trusted execution context using connection/session initialization that is reset safely on connection return, and test pooled-connection reuse. If those controls are not configured, prohibit unreviewed raw SQL against tenant-owned tables. Never interpolate tenant IDs or other values into SQL strings; use parameters.

### Provider qualification matrix

| Capability | PostgreSQL | MariaDB | SQL Server | Oracle | Cosmos |
|---|---|---|---|---|---|
| Central database target routing | Integration-qualified | Integration-qualified | Integration-qualified | Integration-qualified | Separate provider model |
| Tenant-owned tracked reads/writes | Real-engine PostgreSQL coverage | Additional persistence qualification required | Additional persistence qualification required | Additional persistence qualification required | Separate partition/SDK semantics |
| Set-based bulk update | Verify provider/version and translation | Verify provider/version and translation | Verify provider/version and translation | Verify provider/version and translation | Not assumed; provider may not support EF ExecuteUpdate |
| Raw SQL row isolation | Database policy required | Database policy required | Database policy required | Database policy required | Not equivalent to relational raw SQL |

A passing routing test is not evidence of full persistence isolation. A provider's EF query-filter behavior is not evidence that raw SQL, provider-native bulk APIs, stored procedures, or external database clients enforce tenant boundaries.


## Release 2.3.4 qualification notes

The 2.3.4 implementation establishes a fresh DI scope for each supported message delivery and clears tenant execution state when the delivery pipeline exits, including exceptional and cancellation exits. Outbox dispatch resolves its persistence and dispatch services within a per-message scope. The MassTransit Kafka scoped-handler overload is the preferred path when handler dependencies are tenant-sensitive; the legacy captured-handler overload fails closed when tenant-aware processing is enabled.

These guarantees are transport- and registration-path-specific. They do not make untrusted metadata an authorization source, and they do not replace tenant membership or producer authorization. Applications must test their configured adapters and verify that no tenant-sensitive dependency or ambient state escapes the delivery scope.


## Tenant registry caching

Tenant registry caching is opt-in through the caching integration package. Register the application-owned `ITenantStore` first, then decorate it:

```csharp
services.AddSingleton<ITenantStore, SqlTenantStore>();
services.AddTenantStoreCaching(options =>
{
    options.AbsoluteExpiration = TimeSpan.FromMinutes(2);
});
```

The decorator uses process-local `IMemoryCache` entries with absolute expiration. Successful lookups are cached; missing tenants are not negatively cached. Cache population is single-flight per key, and returned tenant metadata is copied so callers cannot mutate cached snapshots through the returned object.

After every successful tenant-registry write, invalidate the affected metadata:

```csharp
var invalidator = serviceProvider.GetRequiredService<ITenantStoreCacheInvalidator>();
invalidator.InvalidateTenant(tenantId);
```

Use `InvalidateTenant(tenantId)` for any tenant metadata update, including changes to its domains; it removes the tenant lookup, existence result, full-registry snapshot, and all cached or in-flight hostname lookups. When a hostname is reassigned, invalidate both affected tenant IDs. Use `InvalidateHost(host)` only when the host lookup entry itself must be purged without changing either tenant record. Use `InvalidateAll()` after bulk imports or registry-wide changes. Invalidation also prevents an in-flight lookup from repopulating an entry after the invalidation boundary.

This cache is process-local, not a distributed coherence protocol. In multi-instance deployments, publish invalidation to every application instance after a registry write, or rely on the configured absolute TTL as the maximum stale-read window. Do not treat TTL as a replacement for explicit invalidation after administrative writes. The default TTL is one minute; configure it according to tenant metadata change frequency and acceptable staleness.

