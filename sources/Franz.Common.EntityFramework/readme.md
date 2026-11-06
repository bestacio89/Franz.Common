# Franz.Common.EntityFramework

> EF Core persistence infrastructure for repositories, unit of work, auditing, soft deletion, concurrency, and tenant isolation.

## Overview

This package contains the provider-neutral EF Core persistence layer used by Franz applications. Database-provider configuration belongs to the corresponding provider packages; this package supplies the common DbContextBase, repository implementations, persistence behavior, and database isolation policy.

## Installation

~~~bash
dotnet add package Franz.Common.EntityFramework
~~~

Install the provider package that matches the database engine as well (for example, Franz.Common.EntityFramework.PostGres for PostgreSQL).

## Core context

Application contexts derive from DbContextBase and expose their entity sets. The base context requires the Franz dispatcher and optionally accepts the current-user service, tenant execution-context accessor, database options, and multi-tenancy options.

The constructor overload that accepts both IOptions<DatabaseOptions> and IOptions<MultiTenancyOptions> is the explicit composition path for applications that enable tenant isolation and/or database routing.

DbContextBase coordinates:

- auditing for IAuditableEntity entries;
- soft deletion for Franz domain entities implementing IEntity<TKey>;
- optimistic concurrency for IConcurrencyTrackedEntity;
- tenant ownership checks on writes to ITenantEntity;
- tenant and soft-delete query-filter composition after the derived context has configured its model.

Deleted auditable entities are converted to modified entries and marked deleted; they are not physically deleted by the base context.

## Query-filter composition and tenant isolation

Tenant isolation is opt-in through MultiTenancyOptions.Enabled. When enabled, tenant-owned entities receive a query predicate based on the current validated execution context. A missing tenant context produces an empty tenant identity for queries (matching no tenant rows) and is rejected when tenant-owned entities are persisted.

The model-finalization convention composes Franz predicates with application filters rather than replacing them. It preserves the application's filter style: anonymous filters remain a single unnamed filter, while named filters remain named. Mixing named and unnamed filters on one entity is rejected because EF Core 10 does not support that combination. The framework filter name is reserved.

For SharedDatabase mode, every mapped IEntity<TKey> must implement exactly one of ITenantEntity or IGlobalEntity. This classification is validated when the model is built. Non-multi-tenant applications do not receive forced tenant filtering.

## Database isolation modes

DatabaseOptions.IsolationMode controls database-name resolution when DatabaseName contains the {dbName} token:

| Mode | Behavior |
| --- | --- |
| SharedDatabase | Uses the configured database name unchanged; rejects a tokenized name. |
| TenantDatabase | Replaces the token with the validated TenantExecutionContext.TenantId; missing tenant context fails closed. |
| DomainDatabase | Replaces the token with IDomainContextAccessor.GetCurrentDomainId(); this is an explicit opt-in, not a tenant fallback. |

DatabaseName must be configured. Tenant routing never falls back to domain identity, and domain routing never happens implicitly.

Example configuration:

~~~json
{
  "Database": {
    "IsolationMode": "TenantDatabase",
    "ServerName": "localhost",
    "DatabaseName": "orders-{dbName}",
    "UserName": "app",
    "Password": "from-secret-configuration",
    "EnableSensitiveDataLogging": false
  }
}
~~~

Keep credentials in a secret store or environment configuration. Sensitive-data logging should remain disabled in production unless explicitly required and reviewed.

## Repository and unit-of-work registration

The registration extensions are in Franz.Common.EntityFramework.Extensions:

~~~csharp
using Franz.Common.EntityFramework.Extensions;

services.AddDatabaseOptions(configuration);
services.AddEntityRepositories<ApplicationDbContext>();
services.AddUnitOfWork<ApplicationDbContext>();
services.AddBehaviors();
~~~

- AddDatabaseOptions binds the Database section and requires configuration unless options have already been registered.
- AddEntityRepositories<TDbContext> discovers public DbSet<TEntity> properties implementing IEntity<TKey>, excludes aggregate roots, and registers the EF entity repository plus the EF bulk-update and bulk-delete capabilities.
- AddUnitOfWork<TDbContext> registers IUnitOfWork backed by EfUnitOfWork<TDbContext>.
- AddBehaviors registers the EF persistence pipeline behavior.

Aggregates are deliberately not auto-scanned. Register aggregate repositories explicitly with AddAggregateRepository<TAggregate, TEvent, TId, TRepository>().

## Bulk operations

The EF-specific bulk update and bulk delete contracts are provider capabilities, not additions to the provider-neutral business repository contract. Bulk updates use EF Core's set-based execution and therefore do not materialize tracked entities or invoke SaveChanges, change-tracker auditing, SaveChanges interceptors, or domain-event dispatch. Callers must set any required audit fields explicitly and must not assume tracked instances are refreshed.

The bulk update implementation rejects attempts to change TenantId for tenant-owned entities and retains the active EF query filters. Provider translation support is required; non-relational providers such as Cosmos may not support these operations.

See [repository bulk-operation semantics](Repositories/README.md) for the detailed contract.

## Provider boundaries

Use the provider-specific packages for database engine integration. Do not assume every EF Core provider supports the same bulk operations, transaction behavior, concurrency mechanism, or query translation. Cosmos uses its native ETag concurrency mechanism; relational providers use the configured concurrency properties.

## License

MIT. See the repository [LICENSE](../../LICENSE).
