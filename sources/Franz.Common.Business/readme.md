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

### **3. Domain-Driven Design (DDD) Support**
- Core building blocks for DDD:
  - **Entities**:
    - Base classes: `Entity<TId>` and `Entity`.
    - Example: `Order`, `Customer`.
  - **Value Objects**:
    - For immutable, equality-driven domain objects.
  - **Enumeration**:
    - Strongly typed enums with metadata and behavior.
  - **Repositories**:
    - Interfaces: `IAggregateRepository`, `IReadRepository`.
  - **AggregateRoot**:
    - Marker interface for aggregates in the domain model.

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

- **Current Version**: 1.2.64
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
dotnet add package Franz.Common.Business --Version 1.2.64
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

### Version 1.2.64

- Upgrade version to .net 9

---

