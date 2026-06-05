# 📦 Franz.Common.EntityFramework v2.2.3

## 🚀 Version

**Current Version:** v2.2.6

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

