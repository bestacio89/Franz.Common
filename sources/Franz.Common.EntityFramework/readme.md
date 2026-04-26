
# 📦 Franz.Common.EntityFramework

A core infrastructure library of the **Franz Framework**, designed to extend and simplify **Entity Framework Core** integration in .NET systems.

It provides a **pragmatic persistence layer**, supporting:

* Domain-Driven Design (selectively applied)
* CQRS architectures
* Soft deletes + auditing
* Event-driven persistence patterns
* DI-driven modular design

Unlike strict DDD implementations, this library follows a **system-first pragmatic architecture**, where domain purity is preserved only where it adds value.

---

# 🚀 Version

**v2.1.2**

---

# 🧠 Architectural Philosophy

This library is built on a **pragmatic DDD enforcement model**:

> DDD is applied where it improves domain correctness, not as a global constraint.

## Core principles:

* Persistence is **EF-native**, not domain-driven
* Repositories are **identity-agnostic (`IEntity`)**
* Domain identity (`Entity<TId>`) is isolated from persistence contracts
* DI is **explicit and convention-based**
* Infrastructure is optimized for **scalability over theoretical purity**

---

# 🧱 Core Concepts

---

# 1. DbContextBase (Canonical EF Context)

The base DbContext provides:

## ✔ Built-in Features

* Auditing (`CreatedBy`, `CreatedOn`, etc.)
* Soft deletes (`IsDeleted`, `DeletedOn`, `DeletedBy`)
* Global query filters
* Domain event dispatching via `IDispatcher`
* Current user tracking integration

---

## 📌 Example

```csharp
public class AppDbContext : DbContextBase
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IDispatcher dispatcher,
        ICurrentUserService currentUser)
        : base(options, dispatcher, currentUser)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
```

---

## 🧠 Behavior Summary

* Automatically applies auditing on `SaveChanges`
* Converts deletes into **soft deletes**
* Filters deleted entities globally
* Dispatches domain events after persistence

---

# 2. Repository System (Pragmatic Model)

The repository system is intentionally simplified for scalability.

---

## 📦 EntityRepository (CRUD)

```csharp
public class EntityRepository<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : class, IEntity
```

### ✔ Responsibilities:

* CRUD operations
* EF Core persistence
* Soft delete compliance
* No domain logic
* No identity assumptions

---

### 📌 Example

```csharp
public class OrderRepository 
    : EntityRepository<AppDbContext, Order>
{
    public OrderRepository(AppDbContext dbContext) 
        : base(dbContext) { }
}
```

---

## 🧠 Key Design Rule

> Repositories do NOT manage identity.

Identity is handled by:

* `IEntity.GetId()`
* EF Core key resolution
* Domain layer (`Entity<TId>`)

---

## 📦 AggregateRepository (Event-Sourced)

Used for event-driven aggregates.

```csharp
public class AggregateRepository<TDbContext, TAggregateRoot, TEvent>
```

### ✔ Responsibilities:

* Event sourcing persistence
* Aggregate rehydration
* Event storage coordination
* Domain event consistency

---

# 3. Auditing System

All entities implementing `Entity<TId>` automatically support:

* `CreatedOn`, `CreatedBy`
* `LastModifiedOn`, `LastModifiedBy`
* `DeletedOn`, `DeletedBy`
* `IsDeleted` soft delete flag

---

## ✔ Behavior

* Automatically applied on `SaveChanges`
* Fully transparent to application layer
* No manual intervention required

---

# 4. Soft Delete System

Instead of physical deletion:

```text
DELETE → UPDATE IsDeleted = true
```

### ✔ Features:

* Global EF query filter
* Automatic exclusion of deleted entities
* Audit trail preserved

---

# 5. Domain Events Integration

DbContext automatically dispatches domain events via:

```csharp
IDispatcher
```

### ✔ Lifecycle:

1. Entity changes tracked
2. SaveChanges executed
3. Events collected
4. Dispatcher publishes after commit

---

# 6. Entity Framework Conventions

### ✔ Supported entity model

```csharp
public abstract class Entity<TId> : IEntity
```

### ✔ Characteristics:

* Strongly typed identity (`TId`)
* EF-compatible mapping
* Audit support (via DbContext)
* Soft delete support

---

### ✔ Identity rules

* Identity is defined in the domain (`Entity<TId>`)
* Repositories are identity-agnostic
* EF resolves identity via `object[]` keys

---

# 7. Dependency Injection Model

This framework uses **explicit DI discovery**.

---

## ✔ Auto-registration rules

Only services implementing:

* `IScopedDependency`
* `ISingletonDependency`

are automatically registered.

---

## ✔ Benefits

* No hidden service locator
* Fully deterministic composition
* Explicit module boundaries
* Scalable modular architecture

---

# 8. Configuration Extensions

## ✔ Register EF Infrastructure

```csharp
services.AddEntityFrameworkFranz();
```

Includes:

* DbContextBase support
* repositories
* auditing pipeline
* DI wiring

---

# ⚠️ Design Constraints

## ❌ Do NOT

* Inject identity logic into repositories
* Use repository-level `TId` constraints
* Bypass `Entity<TId>` model
* Perform manual auditing

---

## ✔ Always

* Let EF handle persistence
* Let domain handle identity
* Use repositories as orchestration layer only
* Use DI markers for registration

---

# 🧭 Architecture Overview

```
Domain Layer
 ├── Entity<TId>
 ├── Domain Events
 ├── Aggregates

Application Layer
 ├── CQRS / Mediator

Infrastructure Layer
 ├── DbContextBase
 ├── EntityRepository
 ├── AggregateRepository

EF Core Layer
 ├── DbSet<TEntity>
 ├── ChangeTracker
 ├── Global Filters

DI Layer
 ├── IScopedDependency
 ├── IServiceCollection extensions
```

---

# 🧪 Version History

## v2.0.3 – Pragmatic Architecture Alignment

### 🧠 Major Changes

* Replaced identity-coupled repositories with `IEntity`-based model
* Removed `TId` dependency from repository contracts
* Standardized EF Core identity resolution via `object`
* Introduced explicit DI-based service discovery model
* Reinforced pragmatic DDD enforcement strategy

### ⚙️ Improvements

* Reduced repository complexity
* Simplified DI wiring
* Improved EF Core compatibility
* Strengthened infrastructure scalability

### ⚠️ Design Trade-off

* Reduced compile-time identity enforcement in repositories
* Identity safety moved to domain layer (`Entity<TId>`)

---

# 📌 Summary

Franz.Common.EntityFramework v2.0.3 provides:

✔ EF-native persistence layer
✔ Pragmatic DDD enforcement
✔ Scalable repository abstraction
✔ Auditing + soft delete system
✔ Event-driven persistence support
✔ DI-driven modular architecture

---

# 🧠 Final Statement

> This library prioritizes system scalability and architectural clarity over theoretical purity, applying DDD only where it improves correctness, not as a global constraint.

---

