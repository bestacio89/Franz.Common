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
- The generic MassTransit Kafka adapter exposes a scoped-handler overload that receives the per-delivery `IServiceProvider`; it maps Kafka headers into the tenant message envelope and invokes the same initializer. Its legacy `Func<T, Task>` overload fails closed when messaging multitenancy is registered, because a captured handler cannot be guaranteed to resolve dependencies from the delivery scope.
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
