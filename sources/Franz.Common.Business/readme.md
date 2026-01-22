# **Franz.Common.Business**

A core library of the **Franz Framework**, designed to facilitate **Domain-Driven Design (DDD)**, **Event Sourcing**, and **CQRS (Command Query Responsibility Segregation)** in .NET applications.
It provides abstractions, utilities, and patterns for building **scalable, auditable, and testable business logic**.

---

* **Current Version**: 1.7.6

---

## **Features**

### **1. Domain-Driven Design (DDD) Building Blocks**

#### **Entities**

Base class for domain objects with **identity, auditing, and lifecycle management** baked in:

* Strongly typed `Id`.
* `PersistentId` (GUID) for cross-system correlation.
* Built-in **audit fields** (`DateCreated`, `LastModifiedDate`, `CreatedBy`, etc.).
* **Soft delete support** (`IsDeleted`, `DateDeleted`, `DeletedBy`).
* Correct **equality semantics** (`==`, `!=`, `Equals`, `GetHashCode`).

```csharp
public abstract class Entity<TId> : IEntity
{
    public TId Id { get; protected set; } = default!;
    public Guid PersistentId { get; private set; } = Guid.NewGuid();

    // Audit
    public DateTime DateCreated { get; private set; }
    public DateTime LastModifiedDate { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public string? LastModifiedBy { get; private set; }

    // Lifecycle
    public bool IsDeleted { get; private set; }
    public DateTime? DateDeleted { get; private set; }
    public string? DeletedBy { get; private set; }

    public void MarkCreated(string createdBy) { ... }
    public void MarkUpdated(string modifiedBy) { ... }
    public void MarkDeleted(string deletedBy) { ... }

    public bool IsTransient() => EqualityComparer<TId>.Default.Equals(Id, default!);

    public override bool Equals(object? obj) { ... }
    public override int GetHashCode() { ... }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => ...
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => ...
}
```

➡️ A non-generic version `Entity : Entity<int>` is also provided for convenience.

---

#### **Value Objects**

Immutable, equality-driven domain concepts.

```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }
}
```

#### **Enumerations**

Strongly typed enums with additional behavior and metadata.

#### **Repositories**

Interfaces for persistence with strict **event-first semantics** (see section below).

---

### **2. Domain Events**

All domain events implement the standardized `IDomainEvent` interface:

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

* Every event is **auditable, traceable, and publish-ready**.
* Provides **correlation + observability metadata** across distributed systems.

---

### **3. Aggregates (Event-Sourced)**

* State modified **only via events** raised with `RaiseEvent()`.
* Built-in **rehydration & replay** for event sourcing.
* Versioning (`Version`) tracked automatically.
* Uncommitted events kept until persistence.

```csharp
public abstract class AggregateRoot<TEvent> : Entity<Guid>, IAggregateRoot<TEvent>
    where TEvent : IEvent
{
    protected void RaiseEvent(TEvent @event) => ApplyChange(@event, true);
    public void ReplayEvents(IEnumerable<TEvent> events) { ... }
    public void Rehydrate(Guid id, IEnumerable<TEvent> events) { ... }
    public IReadOnlyCollection<TEvent> GetUncommittedChanges() { ... }
}
```

---

### **4. Repository Contract**

Persistence is **event-first** with `IAggregateRootRepository`:

```csharp
public interface IAggregateRootRepository<TAggregateRoot, TEvent>
    where TAggregateRoot : class, IAggregateRoot<TEvent>
    where TEvent : IEvent
{
    Task<TAggregateRoot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(TAggregateRoot aggregate, CancellationToken cancellationToken = default);
}
```

* Aggregates are **rehydrated from events**.
* Saves commit **uncommitted domain events**.

---

### **5. CQRS Support**

* Commands (`ICommandRequest<T>`, `ICommandHandler<TCommand,TResponse>`)
* Queries (`IQueryRequest<T>`, `IQueryHandler<TQuery,TResponse>`)
* Built-in Mediator pipelines for resilience + logging.

---

### **6. Resilience Pipelines**

* Retry
* CircuitBreaker
* Timeout
* Bulkhead
  (Configurable via `appsettings.json`).

---

## **What’s New in 1.6.2**

* Added **auditing + soft delete** in `Entity<TId>`.
* Introduced **`IDomainEvent`** with correlation & audit metadata.
* Refactored **AggregateRoot<TEvent>** with strict event sourcing lifecycle.
* Added **IAggregateRootRepository** enforcing event-first persistence.
* Stronger **Mediator semantics** (`SendAsync` vs `PublishAsync`).

---

### **1.6.15**

* Fixed a compile-time error in ReadRepository and IReadRepository caused by an incorrect cast from List<T> to IQueryable<T>.

* Updated GetAll() to return an IReadOnlyCollection<T> for safer read-only semantics instead of forcing a materialized IQueryable<T>.
---

### Version 1.6.20

- Updated to **.NET 10.0**