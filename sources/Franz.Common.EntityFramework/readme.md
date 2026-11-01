# Franz.Common.EntityFramework

Framework-managed EF Core persistence boundaries for Franz.Common.

## Install

```bash
dotnet add package Franz.Common.EntityFramework --version 2.3.4
```

## Persistence model

Provides EF Core integration for framework entities, repositories, unit-of-work orchestration, auditing, soft deletion and domain-event lifecycle. Repositories express persistence intent; transaction ownership belongs to the configured unit-of-work or application pipeline boundary.

## Tenant-owned entities

Tenant-owned entities implement Franz.Common.MultiTenancy.ITenantEntity and expose a mapped Guid TenantId. A derived DbContext must accept and forward the scoped ITenantExecutionContextAccessor to DbContextBase:

```csharp
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDispatcher dispatcher,
    ITenantExecutionContextAccessor tenantContext)
    : DbContextBase(options, dispatcher, tenantContextAccessor: tenantContext)
{
}
```

With validated tenant context, the framework applies tenant-aware global query filters, stamps new tenant-owned entities and rejects cross-tenant inserts/updates/deletes. Queries without tenant context fail closed for tenant-owned entities. Tenant identity is immutable after insertion.

## Bulk operations

The framework repository ExecuteBulkUpdateAsync uses EF Core set-based updates while retaining configured global query filters. The bulk-update builder rejects assignments to TenantId. The shared relational contract verifies tenant-filtered affected rows and ownership-mutation rejection.

Direct EF Core ExecuteUpdateAsync and ExecuteDeleteAsync retain global query filters but bypass materialization, change tracking, SaveChanges, audit hooks and domain-event dispatch. They must not mutate tenant ownership; callers must explicitly manage audit fields and tracked-instance consistency.

## Provider qualification

The shared relational tenant contract is exercised against PostgreSQL 16, MariaDB 11.4, MySQL 8.4, SQL Server 2022 and Oracle Free 23. Provider migrations, transactions, concurrency, SQL translation and configuration are qualified separately. Cosmos DB and MongoDB are not covered by this relational contract.

## Security boundary

Global query filters do not protect raw SQL, stored procedures, IgnoreQueryFilters, direct database connections or provider-native bulk APIs that bypass the configured DbContext/repository. Those paths require a trusted tenant predicate or independent database enforcement such as row-level security. Tenant-scoped unique constraints, composite foreign keys, database permissions and provisioning remain application/schema responsibilities.

Framework release line: **2.3.4**; target framework: net10.0.

See [tenant isolation audit](../../Docs/tenant-isolation-boundary-audit.md), [relational provider contract](../../Docs/relational-provider-contract.md) and [messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md).

MIT License.
