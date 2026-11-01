# Franz.Common.EntityFramework

> Persistence and infrastructure integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.EntityFramework` package provides persistence and infrastructure integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

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

**Current Version:** v2.3.3

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
