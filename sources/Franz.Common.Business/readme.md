
# 📦 Franz.Common.Business

A core infrastructure library of the **Franz Framework**, designed to support **Domain-Driven Design (DDD)**, **CQRS**, and **Event-Sourcing-ready architectures** in modern .NET applications.

It provides a **clean, deterministic, and production-grade foundation** for building scalable business systems with strong separation of concerns.

---

## 🚀 Version

**v2.1.4**

---

## 🧠 Core Philosophy

Franz.Common.Business enforces the following principles:

- Identity is **factory-controlled**
- Domain logic is **persistence-agnostic**
- Repositories are **storage-only abstractions**
- Entities are **immutable in identity after creation**
- DI is **deterministic and explicit**
- No hidden service locator or runtime DI construction

---

## ✨ Key Features

---

## 1. Domain Model (DDD Core)

### ✔ Entity Model

All domain entities derive from a unified base:

```csharp
public abstract class Entity<TId> : IEntity
{
    public TId Id { get; private set; } = default!;

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    public object GetId() => Id!;
}
````

### ✔ Key characteristics:

* Strongly typed identity (`Guid` or `int`)
* Factory-controlled identity assignment
* Immutable identity after creation
* Equality based on identity
* EF Core compatible design

---

## 2. Identity System

### ✔ Supported ID strategies

* `Guid V7` (default standard)
* `int` (database-generated identities)

### ✔ Centralized ID generation

```csharp
public interface IIdGenerator<TId>
{
    TId Create();
}
```

### ✔ Default implementation:

```csharp
public sealed class GuidV7Generator : IIdGenerator<Guid>
{
    public Guid Create() => Guid.CreateVersion7();
}
```

---

## 3. Entity Factories

Entities must be created through controlled factories.

```csharp
public interface IEntityFactory<TId, TEntity>
    where TEntity : Entity<TId>
{
    TEntity Create();
}
```

### ✔ Responsibilities:

* Generate entity identity via `IIdGenerator<TId>`
* Ensure consistent creation rules
* Enforce domain construction invariants
* Prevent identity bypass (e.g. `new Entity()` misuse)

---

## 4. Repositories (Persistence Layer)

Repositories are **identity-agnostic and persistence-only abstractions**.

```csharp
public interface IEntityRepository<TEntity>
    where TEntity : class, IEntity
{
    Task<TEntity> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
```

### ✔ Design principles:

* No dependency on `TId`
* No domain logic
* EF Core handles identity resolution
* Supports both `Guid` and `int` primary keys

---

## 5. Aggregates & Event Sourcing

### ✔ Aggregate Root

Supports event-driven state mutation:

```csharp
public abstract class AggregateRoot<TEvent> : Entity<Guid>
    where TEvent : IEvent
{
    protected void RaiseEvent(TEvent @event) { }
    public void ReplayEvents(IEnumerable<TEvent> events) { }
    public void Rehydrate(Guid id, IEnumerable<TEvent> events) { }
}
```

### ✔ Features:

* Event-based state changes
* Automatic version tracking
* Uncommitted event collection
* Full rehydration support

---

## 6. Domain Events

All events implement:

```csharp
public interface IDomainEvent : IEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    string? CorrelationId { get; }
    Guid? AggregateId { get; }
    string AggregateType { get; }
    string EventType { get; }
}
```

### ✔ Benefits:

* Full auditability
* Distributed tracing support
* Event correlation across services

---

## 7. CQRS Support

Built-in support for:

* Commands (`ICommandHandler`)
* Queries (`IQueryHandler`)
* Mediator-based execution pipeline

---

## 8. Resilience Pipelines

Production-ready pipeline support:

* Retry
* Circuit Breaker
* Timeout
* Bulkhead Isolation

---

## 9. Dependency Injection Bootstrap

### ✔ Single entry point:

```csharp
services.AddBusiness(applicationAssembly);
services.AddBusinessPlatform();
```

### ✔ What it registers:

#### Domain layer

* `IIdGenerator<Guid>`
* `IEntityFactory<,>`

#### Mediator layer

* Command/query pipeline
* Event dispatching

#### Handler discovery

* Automatic handler registration

---

## ⚠️ Important Design Rules

### ❌ Do NOT:

* Generate IDs manually (`Guid.NewGuid()`)
* Bypass factories for entity creation
* Use service locator inside startup/configuration
* Introduce custom repository identity types

### ✔ Always:

* Use factories for entity creation
* Use `IIdGenerator<TId>` for identity
* Treat repositories as persistence-only abstractions

---

## 🧩 Architecture Overview

```
Domain Layer
    ├── Entities (Entity<TId>)
    ├── Aggregates
    ├── Value Objects
    └── Domain Events

Factory Layer
    └── IEntityFactory<TId, TEntity>

Identity Layer
    └── IIdGenerator<TId>

Persistence Layer
    └── IEntityRepository<TEntity>

Application Layer
    └── CQRS + Mediator

Bootstrap Layer
    └── AddBusiness()
```

---

## 🧪 Version History

### v2.0.3 – Architecture Stabilization

* Enforced factory-driven identity model
* Removed mutable identity assignment (`SetId` eliminated)
* Introduced immutable entity identity lifecycle
* Simplified repository abstraction (identity-agnostic)
* Consolidated DI bootstrap into single deterministic entry point
* Removed ServiceProvider creation from configuration pipeline
* Improved EF Core compatibility and consistency
* Strengthened DDD alignment across domain layer

---

## 📌 Summary

Franz.Common.Business v2.0.3 provides a:

> ✔ deterministic
> ✔ factory-driven
> ✔ EF Core compatible
> ✔ DDD-aligned
> ✔ CQRS-ready

foundation for enterprise-grade .NET applications.

---

## 🧠 Final Note

This library enforces **architectural discipline by design**, not convention.

It ensures that:

* identity is always controlled
* domain logic remains pure
* persistence remains isolated
* system composition is deterministic

```

---

