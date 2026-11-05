# Franz.Common.EntityFramework

> Provider-agnostic Entity Framework Core persistence abstractions for repositories, unit-of-work operations, soft deletion, and auditing.

## Overview

Provider-agnostic Entity Framework Core persistence abstractions for repositories, unit-of-work operations, soft deletion, and auditing.

## Installation

```bash
dotnet add package Franz.Common.EntityFramework
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

## 🚀 Version

**Current Version:** v2.3.4

---

# 🧠 Architectural Evolution (IMPORTANT CHANGE)

## ❌ Before (v2.1.4)

> “Pragmatic EF Core persistence library”

## ✅ Now (v2.2.1)

> **Framework-managed transactional persistence engine with opt-in orchestration**

---

# 🧠 Core Philosophy (NEW)

> Persistence is no longer a repository responsibility — it is a **framework-controlled consistency boundary**.

### Key shift:

* Repositories = mutation intent
* UnitOfWork = commit boundary
* Pipeline = implicit transaction scope
* DbContext = execution engine

---

# ⚖️ Transaction Model (NEW CORE CONCEPT)

## Franz now supports 3 execution modes:

---

## 🟢 1. Auto-Commit Mode (Simple Operations)

Repositories may persist immediately for isolated operations:

* CRUD endpoints
* single aggregate updates
* simple queries

⚠️ Not intended for multi-aggregate consistency

---

## 🔵 2. Orchestrated Mode (UnitOfWork)

Explicit transactional boundary:

```csharp
await repo.AddAsync(entity);
await repo.AddRangeAsync(entities);

await unitOfWork.SaveChangesAsync();
```

Used for:

* Skill creation
* Hero creation
* snapshot generation pipelines
* multi-aggregate consistency flows

---

## 🟣 3. Pipeline Mode (Implicit Transaction)

```text
Command → PipelineBehavior → Transaction Scope → Commit
```

Used for:

* CQRS commands
* application layer operations
* validated workflows

---

# 🧱 Core Architecture Concepts (UPDATED)

---

# 1. DbContextBase (Execution Engine)

DbContextBase is now explicitly:

> **The transactional execution engine of the framework**

## Responsibilities:

* Change tracking
* audit lifecycle
* soft delete enforcement
* domain event dispatching
* transaction coordination
* unit-of-work execution target

---

## Key behavior change:

### BEFORE:

“DbContext applies behavior”

### NOW:

“DbContext executes framework-defined transactional rules”

---

# 2. Repository System (REFINED MODEL)

---

## 📦 EntityRepository (Intent-Based Persistence)

```csharp
public class EntityRepository<TDbContext, TEntity>
```

### ✔ Responsibilities:

* Express persistence intent
* Track entity state
* Support batch operations
* NO transaction ownership

---

### ❌ Removed responsibility:

* SaveChanges ownership
* transactional guarantees

---

## 🔥 NEW ADDITIONS (v2.2.1)

### Batch operations added:

* `AddRangeAsync`
* `UpdateRangeAsync`
* `DeleteRangeAsync`
* `SoftDeleteRangeAsync`

---

## 🧠 New rule:

> Repositories describe *what changes*, not *when changes are committed*

---

# 3. Unit of Work (NEW CORE COMPONENT)

## 📦 IUnitOfWork

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## ✔ Role:

* Defines explicit transaction boundary
* Ensures atomic multi-repository operations
* Guarantees deterministic state commits

---

## 🧠 Key principle:

> If multiple aggregates are involved, UnitOfWork is mandatory.

---

# 4. Soft Delete System (UNCHANGED BUT REINFORCED)

Still applies globally:

```text
DELETE → IsDeleted = true
```

### ✔ Now enforced at framework level only

* repositories no longer handle delete semantics
* DbContext enforces policy

---

# 5. Domain Events System (ENHANCED CLARITY)

### Lifecycle:

1. entities modified
2. DbContext tracks changes
3. commit executed
4. events dispatched AFTER transaction completion

---

### 🧠 NEW RULE:

> Events are emitted only after UnitOfWork commit success OR pipeline completion.

---

# 6. Entity Model (UNCHANGED BUT REINFORCED)

Still:

```csharp
public abstract class Entity<TId>
```

### ✔ Guarantees:

* identity ownership stays in domain
* audit is framework-managed
* soft delete is universal

---

# 7. Dependency Injection System (UPDATED SEMANTICS)

## Auto-discovery now explicitly scoped:

* repositories → persistence layer
* behaviors → pipeline layer
* unit of work → opt-in transactional layer

---

## NEW ADDITION:

```csharp
services.AddUnitOfWork<TDbContext>();
```

### Meaning:

> “Enable explicit transactional orchestration mode”

---

# 8. EF Entity Discovery (UNCHANGED)

Still:

* DbSet scanning
* IEntity filtering
* aggregate exclusion

---

# ⚠️ DESIGN RULES (UPDATED)

## ❌ FORBIDDEN

* repositories calling SaveChanges
* implicit multi-aggregate persistence
* mixing transaction boundaries inside domain logic

---

## ✔ REQUIRED

* UnitOfWork for multi-entity consistency
* batch operations for performance
* pipeline for CQRS safety
* DbContext as only persistence executor

---

# 🧭 Updated Architecture Overview

```
DOMAIN LAYER
 ├── Entity<TId>
 ├── Aggregates
 ├── Domain Events

APPLICATION LAYER
 ├── CQRS
 ├── SnapshotResolver
 ├── Orchestration Services

FRAMEWORK LAYER (Franz 2.2.1)
 ├── EntityRepository (intent-only)
 ├── AddRange / UpdateRange / DeleteRange
 ├── UnitOfWork (transaction boundary)
 ├── PersistenceBehavior (pipeline transaction scope)

INFRASTRUCTURE LAYER
 ├── DbContextBase (execution engine)
 ├── ChangeTracker
 ├── Soft Delete filter
 ├── Event dispatcher

EF CORE LAYER
 ├── DbSet<TEntity>
 ├── SQL translation
 ├── transaction execution
```

---

# 🧪 VERSION HISTORY (UPGRADED)

## v2.2.1 — Transactional Framework Evolution

### 🧠 Major Changes

* Removed “repository owns persistence” model
* Introduced UnitOfWork as explicit transaction boundary
* Added batch operations (AddRange / UpdateRange / SoftDeleteRange)
* Redefined DbContext as execution engine
* Formalized pipeline-based transactional safety model

---

### ⚙️ Improvements

* deterministic multi-aggregate consistency
* improved snapshot safety
* reduced hidden side effects
* clearer orchestration boundaries
* scalable for simulation-heavy systems

---

### ⚠️ Breaking Change Conceptually

* SaveChanges is no longer repository-owned
* transaction ownership is now explicit or pipeline-driven

---

# 📌 FINAL SUMMARY

Franz.Common.EntityFramework v2.2.1 is now:

> ✔ not just EF abstraction
> ✔ not just repository simplification
> ❌ not CRUD wrapper anymore
> ✔ a transactional orchestration framework

---

# 🧠 Closing Statement (matches your philosophy)

> This framework no longer abstracts Entity Framework — it formalizes deterministic persistence boundaries over it, ensuring consistency across complex domain simulations and multi-aggregate workflows.

---

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).


## Tenant-owned persistence

Entities that belong to a tenant implement `Franz.Common.MultiTenancy.ITenantEntity` and expose a mapped `Guid TenantId`. When a validated `ITenantExecutionContextAccessor` is registered in the DbContext scope, `DbContextBase` applies tenant-aware global query filters and enforces ownership during inserts, updates, deletes, and synchronous/asynchronous saves.

- New tenant-owned entities are stamped with the current tenant when their tenant ID is empty.
- Explicit cross-tenant inserts and writes are rejected.
- Tenant-owned queries return no rows without a validated tenant context.
- Tenant identity is immutable after insertion.

Global query filters are defense in depth, not a substitute for database permissions. Raw SQL, bulk APIs, and deliberate `IgnoreQueryFilters` usage must be separately controlled. For high-assurance shared-database deployments, enable database row-level security with a transaction-scoped tenant setting and a restricted application database role. Define tenant-scoped unique constraints and composite tenant-aware foreign keys in the application model; the framework does not silently rewrite business uniqueness semantics.


### Register the context accessor in derived DbContexts

A concrete DbContext must accept and forward the scoped accessor to `DbContextBase`; constructor injection does not automatically pass optional base parameters:

```csharp
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDispatcher dispatcher,
    ITenantExecutionContextAccessor tenantContext)
    : DbContextBase(options, dispatcher, tenantContextAccessor: tenantContext)
{
}
```

If the accessor is not forwarded, tenant-owned queries intentionally match no rows and tenant-owned writes fail closed.


## Provider-neutral database isolation policy

Database routing is configured through `Database:IsolationMode` and `Database:DatabaseName`:

- `SharedDatabase` (default) uses a concrete database name unchanged. A `{dbName}` token is rejected rather than silently removed.
- `TenantDatabase` requires `DatabaseName` to contain `{dbName}` and requires a validated tenant execution context. Missing tenant identity fails closed; domain identity is never a fallback.
- `DomainDatabase` explicitly opts in to domain-based database routing. The configured name must contain `{dbName}` and a domain context must be present.

Example:

```json
{
  "Database": {
    "ServerName": "localhost",
    "DatabaseName": "app_{dbName}",
    "IsolationMode": "TenantDatabase",
    "UserName": "app",
    "EnableSensitiveDataLogging": false
  }
}
```

All EF provider adapters use the shared `DatabaseTargetResolver`; adapters construct provider-specific connection strings only. Domain-based routing is an explicit deployment decision, not a compatibility fallback.


## Tenant isolation and bulk-update contract for 2.3.4

The relational provider qualification covers tenant-scoped EF Core reads and writes, fail-closed behavior when tenant execution context is absent, tenant ownership immutability, cross-tenant mutation rejection, query-filter behavior for supported set-based operations, and the repository `ExecuteBulkUpdateAsync` contract. The named relational engines in the qualification are PostgreSQL 16, MariaDB 11.4, MySQL 8.4, SQL Server 2022 and Oracle Free 23.

Do not treat EF Core global query filters as a complete database security boundary. Raw SQL, stored procedures, direct connections, provider-native bulk APIs, and deliberate filter bypasses need explicit tenant enforcement and tests. Cosmos DB and MongoDB are not included in this relational qualification. See the [relational provider contract](../../Docs/relational-provider-contract.md) and [tenant-isolation audit](../../Docs/tenant-isolation-boundary-audit.md).


## Explicit multi-tenancy activation and shared-database classification

Multi-tenancy is an application-level architectural choice. It is independent of `Database:IsolationMode`. Non-multi-tenant applications (for example, a game backend operated by one owner inside one environment) do not need tenant identifiers, tenant/global entity markers, or tenant execution contexts.

The default is disabled:

```json
{
  "MultiTenancy": {
    "Enabled": false
  },
  "Database": {
    "IsolationMode": "SharedDatabase",
    "DatabaseName": "game"
  }
}
```

Registering the HTTP multi-tenancy or messaging multi-tenancy integration enables `MultiTenancyOptions.Enabled`. A host can also configure `MultiTenancyOptions` explicitly. A concrete `DbContext` that uses the classification gate must forward both `IOptions<DatabaseOptions>` and `IOptions<MultiTenancyOptions>`:

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
}
```

When multi-tenancy is enabled with `DatabaseIsolationMode.SharedDatabase`, every persisted `IEntity<TKey>` must declare exactly one ownership classification:

- Tenant-owned entities implement `ITenantEntity` and expose their mapped `Guid TenantId`.
- Cross-tenant entities implement `IGlobalEntity` only when the data is intentionally shared by every tenant.

Entities implementing neither marker, or implementing both, cause EF model initialization to fail with a diagnostic listing the offending CLR types. When multi-tenancy is disabled, this classification gate is not applied, even when the database isolation mode is `SharedDatabase`. `TenantDatabase` and `DomainDatabase` retain their database-routing semantics; the shared-database classification gate does not apply to those modes.

The concrete DbContext must forward the options because optional base-constructor dependencies are not injected automatically. Existing non-multi-tenant DbContexts can continue using the original constructor overload and do not need tenant-specific changes.


## TenantDatabase operational guidance

`DatabaseIsolationMode.TenantDatabase` selects a logical database name by replacing `{dbName}` with the validated tenant GUID. It does not, by itself, provision databases, resolve credentials, run migrations, or manage connection pools. The configured provider adapter and application composition root must use the resolved database target when constructing tenant-specific provider options.

Operational requirements:

- Resolve the tenant only from the validated tenant execution context. Missing context fails closed; do not fall back to a domain, header, route value, or arbitrary job payload.
- Keep `DbContext` and tenant-specific provider options scoped to the tenant execution. Never mutate a singleton connection string or reuse a context across tenant identities.
- Store only a secret reference such as `TenantInfo.ConnectionStringKey` in tenant metadata. Resolve credentials from the configured secret store; do not persist connection strings or credentials in `TenantInfo`.
- Account for one connection pool per distinct effective connection string. Set provider pool limits deliberately, avoid large minimum-pool values per tenant, and budget aggregate connections across all active tenants and application instances.
- Provision tenant databases through an explicit, idempotent provisioning workflow. Apply schema migrations per tenant database under a per-tenant distributed lock or lease so two application instances cannot migrate the same tenant concurrently.
- Bound migration fan-out. Run migrations in controlled batches rather than launching one task per tenant, record per-tenant success/failure, and make retries safe. A failed tenant migration must not prevent unrelated tenants from being processed.
- Do not migrate every tenant database synchronously during ordinary application startup. Use a deployment/provisioning job with observability, retry policy, and a recovery path.
- When rotating credentials or changing a tenant's database target, invalidate cached tenant metadata and any application-level connection/provider-options cache, then ensure old pools are retired according to the provider's lifecycle contract.

The framework's database-name resolver is a routing primitive, not a tenant database lifecycle manager. Validate provisioning, credential resolution, pool behavior, migration fan-out, and recovery against each concrete database provider used in production.


## License

Licensed under the MIT License. See the repository [LICENSE](../../LICENSE) for details.
