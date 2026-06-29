# 📦 Franz.Common.Business

A core infrastructure library of the **Franz Framework**, designed to support **Domain-Driven Design (DDD)**, **CQRS**, and **Event-Sourcing-ready architectures** in modern .NET applications.

It provides a **clean, deterministic, and production-grade foundation** for building scalable business systems with strong separation of concerns.

---

## 🚀 Version

**Current Version:** v2.2.15

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
```

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

### ✔ Implementation: `EntityFactory<TKey, TEntity>`

The default implementation uses a **compiled expression tree delegate**, cached statically per closed generic type, to invoke the entity's protected constructor with zero reflection overhead after startup.

```csharp
public sealed class EntityFactory<TKey, TEntity> : IEntityFactory<TKey, TEntity>
    where TEntity : Entity<TKey>
{
    public TEntity Create() => _activator(_idGenerator.Create());
}
```

#### How it works

| Concern | Mechanism |
|---|---|
| Constructor discovery | `BindingFlags.NonPublic \| Public` — resolves `protected` constructors |
| Delegate compilation | `Expression.Lambda<Func<TKey, TEntity>>(...).Compile()` |
| Caching | Static field — compiled once per `TEntity` type, shared for the application lifetime |
| Null guard | `ArgumentNullException.ThrowIfNull` on the injected `IIdGenerator<TKey>` |
| Misconfiguration | Throws `TypeInitializationException` with an actionable inner message if the required constructor is absent |
| Eager validation | `EntityFactory<TKey, TEntity>.Validate()` triggers the static constructor at DI registration time |

#### Required entity constructor

Every entity must expose a constructor accepting its key type:

```csharp
public class Order : Entity<Guid>
{
    protected Order(Guid id) : base(id) { }
}
```

#### Eager validation at startup

Call `Validate()` from your DI registration extension to surface misconfigured entity types immediately, rather than on first use:

```csharp
services.AddSingleton<IEntityFactory<Guid, Order>, EntityFactory<Guid, Order>>();
EntityFactory<Guid, Order>.Validate();
```

---

## 4. Aggregate Factories

Aggregate roots follow the same factory pattern via `AggregateFactory<TAggregate, TEvent>`.

```csharp
public interface IAggregateFactory<TAggregate>
{
    TAggregate Create();
}
```

### ✔ Implementation: `AggregateFactory<TAggregate, TEvent>`

Mirrors `EntityFactory` exactly — compiled delegate, static cache, fail-fast error messages, and eager validation support — but is scoped to `AggregateRoot<TEvent>` and hardcoded to `Guid` identity, consistent with the event-sourcing model.

```csharp
public sealed class AggregateFactory<TAggregate, TEvent> : IAggregateFactory<TAggregate>
    where TAggregate : AggregateRoot<TEvent>
    where TEvent : IEvent
{
    public TAggregate Create() => _activator(_idGenerator.Create());
}
```

#### Required aggregate constructor

```csharp
public class OrderAggregate : AggregateRoot<OrderEvent>
{
    protected OrderAggregate(Guid id) : base(id) { }
}
```

#### Eager validation at startup

```csharp
services.AddSingleton<IAggregateFactory<OrderAggregate>, AggregateFactory<OrderAggregate, OrderEvent>>();
AggregateFactory<OrderAggregate, OrderEvent>.Validate();
```

---

## 5. Repositories (Persistence Layer)

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

## 6. Aggregates & Event Sourcing

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

## 7. Domain Events

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

## 8. CQRS Support

Built-in support for:

* Commands (`ICommandHandler`)
* Queries (`IQueryHandler`)
* Mediator-based execution pipeline

---

## 9. Resilience Pipelines

Production-ready pipeline support:

* Retry
* Circuit Breaker
* Timeout
* Bulkhead Isolation

---

## 10. Dependency Injection Bootstrap

### ✔ Single entry point:

```csharp
services.AddBusiness(applicationAssembly);
services.AddBusinessPlatform();
```

### ✔ What it registers:

#### Domain layer

* `IIdGenerator<Guid>`
* `IEntityFactory<,>`
* `IAggregateFactory<>`

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
* Define entities without a `protected T(TKey id)` constructor
* Use service locator inside startup/configuration
* Introduce custom repository identity types

### ✔ Always:

* Use factories for entity creation
* Use `IIdGenerator<TId>` for identity
* Define the required single-parameter constructor on every entity and aggregate
* Call `Validate()` at DI registration time to catch constructor mismatches at startup
* Treat repositories as persistence-only abstractions

---

## 🧩 Architecture Overview

```
Domain Layer
    ├── Entities (Entity<TId>)
    ├── Aggregates (AggregateRoot<TEvent>)
    ├── Value Objects
    └── Domain Events

Factory Layer
    ├── IEntityFactory<TId, TEntity>      → EntityFactory<TKey, TEntity>
    └── IAggregateFactory<TAggregate>     → AggregateFactory<TAggregate, TEvent>

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

### v2.2.09 – Factory Hardening

* Replaced reflection-on-every-call pattern with **compiled expression tree delegates** in both `EntityFactory` and `AggregateFactory`
* Delegates are compiled **once per closed generic type** and cached in a static field for the application lifetime — near-native instantiation performance
* Removed the injected `Func<Guid, TAggregate> activator` from `AggregateFactory` — the factory now owns constructor resolution entirely, eliminating a class of registration-site errors
* Added `TypeInitializationException` with a descriptive inner message when the required single-parameter constructor is absent, replacing the previous opaque CLR failure
* Added `ArgumentNullException.ThrowIfNull` guard on injected `IIdGenerator<TKey>` instances
* Added static `Validate()` method to both factories — call at DI registration time to surface misconfigured types at startup rather than on first use
* Removed unused `System.Collections.Concurrent`, `System.Collections.Generic`, and `System.Text` imports from factory files

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

Franz.Common.Business v2.2.13 provides a:

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