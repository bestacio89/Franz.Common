# **Franz.Common.Business**

A core library of the **Franz Framework**, designed to facilitate **Domain-Driven Design (DDD)** and **CQRS (Command Query Responsibility Segregation)** in .NET applications.
It provides abstractions, utilities, and patterns for building scalable, maintainable, and testable business logic.

---
- **Current Version**: 1.4.2
---
## **Features**

### **1. Domain-Driven Design (DDD) Building Blocks**

* **Entities**: Represent unique objects identified by a primary key.
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
        // Optional: override mediator defaults
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

> These pipelines are implemented in **`Franz.Common.Mediator.Pipelines.Resilience`**.
> `Franz.Common.Business` just **activates them** for you.

---

### **7. appsettings.json Configuration**

Each pipeline reads its **Options** from configuration. Example:

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

The configuration maps directly into Mediator options classes:

* `Retry` → `RetryOptions`
* `CircuitBreaker` → `CircuitBreakerOptions`
* `Timeout` → `TimeoutOptions`
* `Bulkhead` → `BulkheadOptions`

Each pipeline consumes its corresponding options at runtime, deciding whether to execute, skip, or short-circuit requests.

---

### **9. Logging**

On startup you’ll see log messages confirming bootstrap:

```
[INF] ✅ Franz.Business bootstrapped with MyProduct.Application, Version=1.0.0.0
[INF] 🛡️ Resilience pipelines registered: Retry, CircuitBreaker, Timeout, Bulkhead
[WRN] ⚠️ No Application assembly found for MyProduct.Application, Business layer not registered.
```

---

## **New in 1.4.1**

* 🆕 Independent from MediatR → runs fully on **Franz.Common.Mediator**.
* 🆕 Entities & Aggregates: lifecycle tracking (audit fields, soft delete, optimistic concurrency).
* 🆕 Value Objects: strongly typed equality (`IEquatable<T>`), improved hash safety.
* 🆕 Enumerations: cached reflection, type-safe comparisons.
* 🆕 Domain Events: immutable with `init` properties, enriched with metadata for structured logs.
* 🆕 Scrutor-powered DI: auto-discovery of handlers and services with zero boilerplate.

---

## **Dependencies**

* **Scrutor** (4.2.2) – assembly scanning & DI.
* **Microsoft.Extensions.DependencyInjection** (9.0.0) – DI support.
* **Franz.Common.Mediator** – CQRS + pipelines.
* **Franz.Common.Errors** – standardized error handling.

---

## **Installation**

```bash
dotnet add package Franz.Common.Business --version 1.4.1
```

> ⚠️ From **1.4.1**, **MediatR is no longer required**.
> `Franz.Common.Business` uses **Franz.Common.Mediator** internally.

---

## **Contributing**

This package is part of the private **Franz Framework**.
Authorized contributors:

1. Clone [repo](https://github.com/bestacio89/Franz.Common/).
2. Create a feature branch.
3. Submit a PR for review.

---

## **Changelog**

### **1.4.1**

* Removed dependency on MediatR → replaced by **Franz.Common.Mediator**.
* Entities & Aggregates: lifecycle tracking, soft delete, domain events deduplication.
* Value Objects: strongly typed generics, safer equality & hash codes.
* Enumerations: type-safe comparison, cached reflection.
* Domain Events: immutable (`init`), enriched with metadata (`EventId`, `CorrelationId`).
* Scrutor-powered auto-discovery of handlers.
* Clearer separation between **Business** and **Mediator** layers.

### **1.3**

* Upgraded to .NET 9.
* Compatible with in-house mediator library & MediatR.

### **1.2.65**

* Aggregates redesigned to use **event sourcing**.
* All aggregates now require a `Guid` identifier.

---

