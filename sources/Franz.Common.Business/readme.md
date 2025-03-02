# **Franz.Common.Business**

A powerful library within the **Franz Framework**, designed to facilitate **domain-driven design (DDD)** and **CQRS (Command Query Responsibility Segregation)** in .NET applications. This package provides abstractions, utilities, and patterns for building scalable, maintainable, and testable business logic.

---

## **Features**

### **1. Command Handlers**
- Interfaces for defining and handling commands:
  - `ICommandRequest<TResult>`
  - `ICommandBaseRequest`
  - `ICommandHandler<TCommand, TResult>`
  - `ICommandHandler<TCommand>` (void result)

### **2. Queries**
- Abstractions for query handling:
  - `IQueryRequest<TResult>`
  - `IQueryHandler<TRequest, TResult>`

3. Domain-Driven Design (DDD) Support
This package includes core building blocks for Domain-Driven Design.

✅ Entities
Entities represent unique objects in the system, identified by a primary key (TId).

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
✅ Value Objects
Value objects are immutable, equality-driven domain concepts.

```csharp
Copy
Edit
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
✅ Enumerations
Enums with behavior and metadata support.

✅ Repositories
Interfaces for handling data persistence:

IAggregateRepository<TAggregateRoot>
IReadRepository<T>

🆕 4. Working with Aggregates
New in version 1.2.65
Aggregates now follow a strict event-driven model. Unlike regular entities, they:

Enforce consistency across related entities.
Use event sourcing to track and replay changes.
Always have a Guid ID.
🔹 Aggregate Example: Product
```csharp
Copy
Edit
public class Product : EventSourcedAggregateRoot<ProductEvent>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    private readonly List<ProductReview> _reviews = new();
    public IReadOnlyCollection<ProductReview> Reviews => _reviews.AsReadOnly();

    private Product() { }

    public Product(Guid id, string name, decimal price) : base(id)
    {
        RaiseEvent(new ProductCreatedEvent(id, name, price));
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new InvalidOperationException("Price must be positive.");

        RaiseEvent(new ProductPriceUpdatedEvent(Id, newPrice));
    }

    public void AddReview(string comment, int rating)
    {
        if (rating < 1 || rating > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");

        RaiseEvent(new ProductReviewAddedEvent(Id, comment, rating));
    }

    public void Apply(ProductCreatedEvent @event)
    {
        Id = @event.ProductId;
        Name = @event.Name;
        Price = @event.Price;
    }

    public void Apply(ProductPriceUpdatedEvent @event)
    {
        Price = @event.NewPrice;
    }

    public void Apply(ProductReviewAddedEvent @event)
    {
        _reviews.Add(new ProductReview(@event.Comment, @event.Rating));
    }
}
```
5. Event Handling in Aggregates
Aggregates don’t allow direct state modifications. Instead, changes are applied through events.

Event Definitions
```csharp
public class ProductCreatedEvent : BaseEvent
{
    public Guid ProductId { get; }
    public string Name { get; }
    public decimal Price { get; }

    public ProductCreatedEvent(Guid productId, string name, decimal price)
    {
        ProductId = productId;
        Name = name;
        Price = price;
    }
}

public class ProductPriceUpdatedEvent : BaseEvent
{
    public Guid ProductId { get; }
    public decimal NewPrice { get; }

    public ProductPriceUpdatedEvent(Guid productId, decimal newPrice)
    {
        ProductId = productId;
        NewPrice = newPrice;
    }
}

public class ProductReviewAddedEvent : BaseEvent
{
    public Guid ProductId { get; }
    public string Comment { get; }
    public int Rating { get; }

    public ProductReviewAddedEvent(Guid productId, string comment, int rating)
    {
        ProductId = productId;
        Comment = comment;
        Rating = rating;
    }
}


```
Changelog
Version 1.2.65
Introduced explicit separation between Entities and Aggregates.
Implemented event-based aggregate structure.
Upgraded to .NET 9.


### **4. Event-Driven Architecture**
- Event abstractions for integrating and handling domain and integration events:
  - `IEvent`
  - `IEventHandler<TEvent>`
  - `IntegrationEvent` (specific to cross-system communication)
  - `BaseEvent` (common properties for all events)

### **5. Extensions**
- Utility methods for simplifying development:
  - `ServiceCollectionExtensions`: Streamlines DI for handlers.
  - `TypeExtensions`: Reflection and type utilities for advanced scenarios.

### **6. Helpers**
- `HandlerCollector`: Automatically discovers and registers command, query, and event handlers for dependency injection.

---

## **Dependencies**

This package relies on:
- **MediatR** (12.2.0): For mediator-based communication between commands, queries, and events.
- **Scrutor** (4.2.2): For assembly scanning and DI registration.
- **Microsoft.Extensions.DependencyInjection** (8.0.0): For DI support.
- **Franz.Common.DependencyInjection**: Core DI patterns.
- **Franz.Common.Errors**: For standardized error handling and exceptions.

---

## **Version Information**

- **Current Version**: 1.2.65
- Part of the private **Franz Framework** ecosystem.

---

## **Installation**

### **From Private Azure Feed**
Since this package is hosted privately, configure your NuGet client:

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

Install the package:

```bash
dotnet add package Franz.Common.Business --Version 1.2.65
```

---

## **Usage**

### **1. Commands**

Define a command using `ICommandRequest` and handle it with `ICommandHandler`:

```csharp
using Franz.Common.Business.Commands;

public class CreateOrderCommand : ICommandRequest<Guid>
{
    public string CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Business logic for creating an order
        var orderId = Guid.NewGuid();
        Console.WriteLine($"Order created for {request.CustomerName} with ID: {orderId}");
        return orderId;
    }
}
```

---

### **2. Queries**

Define a query using `IQueryRequest` and handle it with `IQueryHandler`:

```csharp
using Franz.Common.Business.Queries;

public class GetOrderByIdQuery : IQueryRequest<Order>
{
    public Guid OrderId { get; set; }
}

public class GetOrderByIdHandler : IQueryHandler<GetOrderByIdQuery, Order>
{
    public async Task<Order> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        // Fetch the order based on OrderId
        return new Order(request.OrderId, "CustomerName", 100.00m);
    }
}
```

---

### **3. Domain-Driven Design**

#### **Entity**
Define a domain entity using the `Entity<TId>` base class:

```csharp
using Franz.Common.Business.Domain;

public class Order : Entity<Guid>, IAggregateRoot
{
    public string CustomerName { get; private set; }
    public decimal TotalAmount { get; private set; }

    public Order(Guid orderId, string customerName, decimal totalAmount)
    {
        Id = orderId;
        CustomerName = customerName;
        TotalAmount = totalAmount;
        DateCreated = DateTime.UtcNow;
    }
}
```

---

#### **ValueObject**
Create immutable, equality-driven value objects using `ValueObject`:

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

### **4. Events**

Define an event and handle it using `IEvent` and `IEventHandler`:

```csharp
using Franz.Common.Business.Events;

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

### **5. Dependency Injection**

Register all handlers and services automatically with `ServiceCollectionExtensions`:

```csharp
using Franz.Common.Business.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddBusinessServices(); // Registers commands, queries, and event handlers
    }
}
```

---

## **Contributing**

This package is part of the private **Franz Framework**. Contributions are restricted to internal development teams. If you are authorized to contribute:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.2.65

- Upgrade version to .net 9

---

