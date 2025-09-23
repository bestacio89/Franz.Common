# **Franz.Common.Business**

A core library of the **Franz Framework**, designed to facilitate **Domain-Driven Design (DDD)** and **CQRS (Command Query Responsibility Segregation)** in .NET applications.
It provides abstractions, utilities, and patterns for building scalable, maintainable, and testable business logic.

---

* **Current Version**: 1.5.3

---

## **Features**

### **1. Domain-Driven Design (DDD) Building Blocks**

* **Entities**: Represent unique objects identified by a primary key, with auditing + soft delete built-in.
* **Value Objects**: Immutable, equality-driven domain concepts.
* **Enumerations**: Strongly typed enums with behavior and metadata.
* **Repositories**: Interfaces for persistence (`IAggregateRepository<TAggregateRoot>`, `IReadRepository<T>`).

**Entity Example:**

```csharp
public class Order : Entity<Guid>
{
    public string CustomerName { get; private set; }
    public decimal TotalAmount { get; private set; }

    public Order(Guid orderId, string customerName, decimal totalAmount)
    {
        Id = orderId;
        CustomerName = customerName;
        TotalAmount = totalAmount;
    }
}
```

**Value Object Example:**

```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }

    public Address(string street, string city, string postalCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }
}
```

---

### **2. Aggregates (Event-Sourced)**

🆕 **Since 1.2.65**, aggregates follow a strict event-driven model:

* Enforce consistency across related entities.
* State is modified **only through events**.
* Support replay via event sourcing.
* Always identified by `Guid`.

Example:

```csharp
public class Product : EventSourcedAggregateRoot<ProductEvent>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    private readonly List<ProductReview> _reviews = new();
    public IReadOnlyCollection<ProductReview> Reviews => _reviews.AsReadOnly();

    public Product(Guid id, string name, decimal price) : base(id)
    {
        RaiseEvent(new ProductCreatedEvent(id, name, price));
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0) throw new InvalidOperationException("Price must be positive.");
        RaiseEvent(new ProductPriceUpdatedEvent(Id, newPrice));
    }

    public void AddReview(string comment, int rating)
    {
        if (rating < 1 || rating > 5) throw new InvalidOperationException("Rating must be between 1 and 5.");
        RaiseEvent(new ProductReviewAddedEvent(Id, comment, rating));
    }

    public void Apply(ProductCreatedEvent @event) { Id = @event.ProductId; Name = @event.Name; Price = @event.Price; }
    public void Apply(ProductPriceUpdatedEvent @event) { Price = @event.NewPrice; }
    public void Apply(ProductReviewAddedEvent @event) { _reviews.Add(new ProductReview(@event.Comment, @event.Rating)); }
}
```

---

### **3. Events**

Supports both **domain events** and **integration events**.

* `IEvent`
* `IEventHandler<TEvent>`
* `IntegrationEvent`
* `BaseEvent`

Example:

```csharp
public class OrderCreatedEvent : IEvent
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; }
}

public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Order created for {@event.CustomerName}.");
    }
}
```

---

### **4. CQRS Support**

* **Commands** (`ICommandRequest<T>`, `ICommandHandler<TCommand,TResponse>`)
* **Queries** (`IQueryRequest<T>`, `IQueryHandler<TQuery,TResponse>`)

**Command Example:**

```csharp
public class CreateOrderCommand : ICommandRequest<Guid>
{
    public string CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        Console.WriteLine($"Order created for {request.CustomerName} with ID: {orderId}");
        return orderId;
    }
}
```

---

### **5. Registration Examples**

The Business layer integrates seamlessly with the **Mediator** and resilience pipelines.

#### **a) AddBusinessWithMediator**

Strict registration – will throw if your `*.Application` assembly is not found.

```csharp
builder.Services.AddBusinessWithMediator(typeof(Program).Assembly);
```

#### **b) TryAddBusinessWithMediator**

Soft registration – logs a warning but continues if the application assembly is missing.

```csharp
builder.Services.TryAddBusinessWithMediator(typeof(Program).Assembly);
```

#### **c) AddFranzPlatform**

Full-stack registration – Business + Mediator + Logging + Resilience pipelines.

```csharp
builder.Services.AddFranzPlatform(
    typeof(Program).Assembly,
    options =>
    {
        options.DefaultTimeout = TimeSpan.FromSeconds(30);
    });
```

---

### **6. Resilience Pipelines (via Mediator)**

When you call `AddFranzPlatform`, `Franz.Common.Business` wires up the following **Mediator pipelines**:

* ✅ `RetryPipeline<TRequest, TResponse>`
* ✅ `CircuitBreakerPipeline<TRequest, TResponse>`
* ✅ `TimeoutPipeline<TRequest, TResponse>`
* ✅ `BulkheadPipeline<TRequest, TResponse>`

> These are implemented in **`Franz.Common.Mediator.Pipelines.Resilience`**.
> `Franz.Common.Business` just **activates them** for you.

---

### **7. appsettings.json Configuration**

Each pipeline reads its **Options** from configuration:

```json
{
  "Franz": {
    "Resilience": {
      "Retry": {
        "Enabled": true,
        "RetryCount": 3,
        "BaseDelayMilliseconds": 200
      },
      "CircuitBreaker": {
        "Enabled": true,
        "FailureThreshold": 5,
        "OpenDurationSeconds": 60
      },
      "Timeout": {
        "Enabled": true,
        "TimeoutSeconds": 15
      },
      "Bulkhead": {
        "Enabled": true,
        "MaxParallelization": 50,
        "MaxQueuingActions": 100
      }
    }
  }
}
```

---

### **8. Options Mapping**

* `Retry` → `RetryOptions`
* `CircuitBreaker` → `CircuitBreakerOptions`
* `Timeout` → `TimeoutOptions`
* `Bulkhead` → `BulkheadOptions`

---

### **9. Logging**

On startup you’ll see log messages confirming bootstrap:

```
[INF] ✅ Franz.Business bootstrapped with MyProduct.Application, Version=1.0.0.0
[INF] 🛡️ Resilience pipelines registered: Retry, CircuitBreaker, Timeout, Bulkhead
[WRN] ⚠️ No Application assembly found for MyProduct.Application, Business layer not registered.
```

---

## **What’s New in 1.4.2**

* 🗑️ Removed **`SaveEntitiesAsync`** → all auditing + domain event dispatching happens in `SaveChangesAsync`.
* ✅ Aligned with **DbContextBase** from `Franz.Common.EntityFramework`.
* 🧹 Internal cleanup and consistency improvements.

---

## **Dependencies**

* **Scrutor** (4.2.2) – assembly scanning & DI.
* **Microsoft.Extensions.DependencyInjection** (9.0.0) – DI support.
* **Franz.Common.Mediator** – CQRS + pipelines.
* **Franz.Common.Errors** – standardized error handling.

---

## **Installation**

```bash
dotnet add package Franz.Common.Business --version 1.4.2
```

> ⚠️ Since **1.4.1**, **MediatR is no longer required**.
> Uses **Franz.Common.Mediator** internally.

---

## **Contributing**

This package is part of the private **Franz Framework**.

1. Clone [repo](https://github.com/bestacio89/Franz.Common/).
2. Create a feature branch.
3. Submit a PR for review.

---

## **License**

Licensed under the **MIT License**.

---

## **Changelog**

Got it ✅ — let’s package those changes into a **README-style changelog log** so you can drop it straight into the repo alongside the code.

Here’s a draft for `Franz.Common.Business v1.4.5`:

---

# **Franz.Common.Business — Release Notes**

## **Version 1.4.5**

*A maintenance release focusing on consistency and event publishability.*

### 🔹 Changes

#### **AggregateRoot\<TEvent>**

* Enforced **publishable events** by constraining

  ```csharp
  where TEvent : BaseDomainEvent, INotification
  ```

  Ensures all domain events can be published via the Mediator pipeline.

* Updated `GetUncommittedChanges()` signature:

  ```csharp
  public IReadOnlyCollection<BaseDomainEvent> GetUncommittedChanges()
  ```

  * Previously returned `IEnumerable<TEvent>`.
  * Now returns an immutable, standardized collection of `BaseDomainEvent`.

---

#### **IAggregateRoot**

* Updated contract for `GetUncommittedChanges()`:

  ```csharp
  IReadOnlyCollection<BaseDomainEvent> GetUncommittedChanges();
  ```

  Guarantees consistency with `AggregateRoot`.

* Retains:

  * `void MarkChangesAsCommitted();`
  * `Guid Id { get; }`

---

### ✅ Benefits

* **Consistency** across AggregateRoot and IAggregateRoot.
* **Safety** via `IReadOnlyCollection` (prevents external mutation).
* **Alignment** with MediatR’s `INotification` pattern, enforcing correct event semantics.

---


### **1.4.2**

* Removed `SaveEntitiesAsync` → replaced with `SaveChangesAsync` in `DbContextBase`.
* Improved alignment with `EntityFramework` package (auditing + domain events).

### **1.4.1**

* Independent from MediatR → runs on `Franz.Common.Mediator`.
* Lifecycle tracking for entities & aggregates.
* Stronger value object equality.
* Enriched domain events with metadata.
* Scrutor-powered DI auto-discovery.

Got it ✅ — let’s make **individual README entries** for each package you’ve patched in `v1.4.5`.
You’ll be able to drop these into each project’s README (under a **Release Notes** or **Changelog** section).

---

## 📌 `Franz.Common.Business`

### **v1.4.5**

*Consistency and publishability improvements for AggregateRoots.*

* Enforced **publishable events** by requiring `TEvent : BaseDomainEvent, INotification`.
* Updated `GetUncommittedChanges()` to return `IReadOnlyCollection<BaseDomainEvent>` instead of `IEnumerable<TEvent>`.
* Updated `IAggregateRoot` contract for consistency with `AggregateRoot`.
* Improved safety (read-only collections) and **alignment with MediatR’s `INotification` pattern**.

---

## 📌 `Franz.Common.EntityFramework`

### **v1.4.5**

*Fixed event dispatch semantics for domain persistence.*

* Replaced `dispatcher.Send(...)` with `dispatcher.PublishAsync(...)` when dispatching domain events.
* Applied fix to both standard and cancellation-token aware variants.
* Ensures domain events behave as **fan-out notifications** instead of commands.

---

## 📌 `Franz.Common.Mediator`

### **v1.4.5**

*Corrected mediator semantics for commands, queries, and events.*

* Introduced clear split:

  * **Commands/Queries** → `SendAsync` (single handler, request/response).
  * **Events (Domain + Integration)** → `PublishAsync` (fan-out, fire-and-forget).
* Fixed mis-implementation where integration events were incorrectly treated as commands.
* Ensures **consistent message handling semantics** across the framework.

---

## 📌 `Franz.Common.Messaging.Hosting.Mediator`

### **v1.4.5**

*Fixed event dispatch handling in hosting layer.*

* `IIntegrationEvent` now dispatched via `PublishAsync(...)` instead of `Send(...)`.
* Commands and queries remain dispatched via `SendAsync(...)`.
* Aligns hosting mediator with proper notification semantics.

---

## 📌 `Franz.Common.Messaging.Kafka`

### **v1.4.5**

*Corrected Kafka integration event pipeline.*

* Changed mediator dispatch from `Send(message)` to `PublishAsync(message)`.
* Ensures Kafka-published integration events follow **publish/notify semantics** instead of command semantics.
* Provides consistent event behavior across transports.

