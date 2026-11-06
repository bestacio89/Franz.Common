# Franz.Common.EntityFramework

> EF Core persistence infrastructure for Franz entities, repositories, unit-of-work transactions, auditing, soft deletion, tenant filters, and optimistic concurrency.

## Overview

The package provides the provider-neutral EF Core base context and repository infrastructure. Database connection configuration is supplied by a provider adapter such as PostgreSQL, MariaDB, Oracle, or SQL Server.

## Installation

```bash
dotnet add package Franz.Common.EntityFramework
```

## Configure a DbContext

A concrete context derives from `DbContextBase` and forwards the execution dependencies it uses. For multi-tenant applications, forward the tenant execution accessor and both option objects so model configuration can apply the intended isolation policy.

```csharp
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDispatcher dispatcher,
    ITenantExecutionContextAccessor tenantContext,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<MultiTenancyOptions> multiTenancyOptions)
    : DbContextBase(
        options,
        dispatcher,
        tenantContextAccessor: tenantContext,
        databaseOptions: databaseOptions,
        multiTenancyOptions: multiTenancyOptions)
{
    public DbSet<Customer> Customers => Set<Customer>();
}
```

For a non-multi-tenant application, the original constructor overload can be used; tenant-specific option forwarding is not required.

## Register provider and repositories

Example with PostgreSQL:

```csharp
using Franz.Common.EntityFramework.Extensions;
using Franz.Common.EntityFramework.Postgres.Extensions;

builder.Services
    .AddPostgresDatabase<ApplicationDbContext>(builder.Configuration)
    .AddEntityRepositories<ApplicationDbContext>()
    .AddUnitOfWork<ApplicationDbContext>();
```

Provider adapters share `DatabaseOptions` and `DatabaseTargetResolver`; each adapter constructs its own provider connection configuration. See the provider package guide for the correct extension method and configuration.

## Repository behavior

`AddEntityRepositories<TDbContext>()` discovers EF `DbSet<TEntity>` properties whose entity types implement `IEntity<TKey>`, excluding aggregate roots. It registers `IEntityRepository<TEntity, TKey>` and the EF bulk-update and bulk-delete contracts.

The standard repository CRUD methods call `SaveChangesAsync`. Repository discovery inspects the concrete DbContext's public `DbSet<TEntity>` properties at runtime and excludes aggregate roots; this registration is reflection-based and is not a Native AOT guarantee. Aggregate repositories are deliberately explicit and are registered with `AddAggregateRepository<TAggregate, TEvent, TId, TRepository>()`. Use an explicit `IUnitOfWork` transaction when multiple operations must commit atomically:

```csharp
await unitOfWork.BeginAsync(cancellationToken);
try
{
    await customerRepository.AddAsync(customer, cancellationToken);
    await orderRepository.AddAsync(order, cancellationToken);
    await unitOfWork.CommitAsync(cancellationToken);
}
catch
{
    await unitOfWork.RollbackAsync(CancellationToken.None);
    throw;
}
```

The unit of work owns the transaction it begins. It rejects an already-active DbContext transaction that it does not own. It commits or rolls back and releases its transaction; it does not create a distributed transaction across separate DbContexts or databases.

## Persistence behavior

`DbContextBase` applies the following framework behavior during synchronous and asynchronous saves:

- Audit lifecycle updates for `IAuditableEntity`.
- Tenant ownership validation for `ITenantEntity` when a validated tenant execution context is present.
- Optimistic concurrency version increments for `IConcurrencyTrackedEntity`.
- Soft-delete handling for tracked entity deletions.

The model-finalization convention composes application query filters with the framework soft-delete predicate. Tenant filters are applied to tenant-owned entities only when multi-tenancy is explicitly enabled. In a shared database with multi-tenancy enabled, every `IEntity<TKey>` must implement exactly one of `ITenantEntity` or `IGlobalEntity`.

Tenant-owned writes fail closed without a validated tenant context. The framework does not infer tenant identity from raw headers or arbitrary claims.

## Set-based operations

`IEntityFrameworkBulkUpdateRepository<TEntity>` exposes `ExecuteBulkUpdateAsync`; `IEntityFrameworkBulkDeleteRepository<TEntity>` exposes `ExecuteBulkDeleteAsync`. These use EF Core set-based operations and do not materialize each matching entity.

Bulk updates add framework audit metadata and increment `ConcurrencyVersion` for concurrency-tracked entities. For relational providers, `TenantId` is also configured as an EF concurrency token; the Cosmos provider retains its native `_etag` concurrency behavior. They reject changes to tenant identity and framework-managed lifecycle/audit fields. Bulk delete is soft-delete by default; pass `hardDelete: true` for physical deletion. Set-based operations bypass the normal change tracker, so test provider translation and transaction behavior for the concrete provider. Important distinction: the current tracked `EntityRepository.DeleteAsync(..., hardDelete: true)` and `DeleteRangeAsync(..., hardDelete: true)` paths call `SaveChangesAsync`, where `DbContextBase` converts deleted auditable entities back to soft-deleted modified entities. Use the bulk hard-delete operation for physical deletion until that tracked-delete interaction is corrected.

## Database isolation modes

`Database:IsolationMode` supports:

- `SharedDatabase`: use the configured database name unchanged.
- `TenantDatabase`: resolve the database name using the validated tenant execution context and the configured `{dbName}` token.
- `DomainDatabase`: explicitly resolve the database name using a validated domain context and the configured token.

Database routing is not a provisioning or migration system. The application owns tenant database provisioning, credential resolution, pool budgeting, and migration orchestration.

## License

MIT. See the repository [LICENSE](../../LICENSE).
