---

# **Franz.Common.Business**

A core library of the **Franz Framework**, designed to facilitate **Domain-Driven Design (DDD)** and **CQRS (Command Query Responsibility Segregation)** in .NET applications.
It provides abstractions, utilities, and patterns for building scalable, maintainable, and testable business logic.

---

## **Features**

### **1. Domain-Driven Design (DDD) Building Blocks**

* **Entities**: Represent unique objects identified by a primary key.
* **Value Objects**: Immutable, equality-driven domain concepts.
* **Enumerations**: Strongly typed enums with behavior and metadata.
* **Repositories**: Interfaces for persistence (`IAggregateRepository<TAggregateRoot>`, `IReadRepository<T>`).

Example **Entity**:

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

Example **Value Object**:

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
Core abstractions:

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

Command Example:

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

### **5. Extensions & Utilities**

* **`ServiceCollectionExtensions`**: Registers command, query, and event handlers with DI.
* **`HandlerCollector`**: Auto-discovers handlers.
* **`TypeExtensions`**: Reflection and type utilities.

```csharp
services.AddBusinessServices(); // Registers all handlers automatically
```

---

## **Dependencies**

* **Scrutor** (4.2.2) – assembly scanning & DI.
* **Microsoft.Extensions.DependencyInjection** (9.0.0) – DI support.
* **Franz.Common.DependencyInjection** – core DI patterns.
* **Franz.Common.Errors** – standardized error handling.

---

## **Installation**

### From Private Azure Feed

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

```bash
dotnet add package Franz.Common.Business --Version 
```

---

## **Contributing**

This package is part of the private **Franz Framework**.
Authorized contributors:

1. Clone [repo](https://github.com/bestacio89/Franz.Common/).
2. Create a feature branch.
3. Submit a PR for review.

---

## **Changelog**

### **1.3**

* Upgraded to .NET 9
* Compatible with in-house mediator library & MediatR
* Clear separation between **business** and **mediator** concepts

### **1.2.65**

* Aggregates redesigned to use **event sourcing**
* All aggregates now require a `Guid` identifier

