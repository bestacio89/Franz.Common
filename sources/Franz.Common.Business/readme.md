# **Franz.Common.Business**

A powerful library within the **Franz Framework**, designed to facilitate **domain-driven design (DDD)** and **CQRS (Command Query Responsibility Segregation)** in .NET applications. This package provides abstractions, utilities, and patterns for building scalable, maintainable, and testable business logic.

---

## **Features**

- **Command Handlers**:
  - Interfaces for defining and handling commands (`ICommandRequest`, `ICommandBaseRequest`, `ICommandHandler`).
- **Queries**:
  - Abstractions for query handling (`IQueryRequest`, `IQueryHandler`).
- **Domain-Driven Design (DDD) Support**:
  - Core building blocks for DDD:
    - `AggregateRoot`
    - `Entity`
    - `ValueObject`
    - `Enumeration`
    - Repositories (`IReadRepository`, `IAggregateRepository`).
- **Event-Driven Architecture**:
  - Event abstractions for integrating and handling domain and integration events:
    - `IEvent`
    - `IEventHandler`
    - `IntegrationEvent`.
- **Extensions**:
  - `ServiceCollectionExtensions` for dependency injection.
  - `TypeExtensions` for reflection and type utilities.
- **Helpers**:
  - `HandlerCollector` to streamline command, query, and event handler discovery and registration.

---

## **Dependencies**

This package relies on:
- **MediatR** (12.2.0): For mediator-based communication between commands, queries, and events.
- **Scrutor** (4.2.2): For assembly scanning and dependency injection.
- **Microsoft.Extensions.DependencyInjection** (8.0.0): For DI support.
- **Franz.Common.DependencyInjection**: Provides core DI patterns.
- **Franz.Common.Errors**: For standardized error handling and exceptions.

---

## **Version Information**

- **Current Version**: 1.2.62
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
dotnet add package Franz.Common.Business --version 1.2.62
```

---

## **Usage**

### **1. Commands**

Define a command using the `ICommandRequest` and handle it with the `ICommandHandler`:

```csharp
using Franz.Common.Business.Commands;

public class CreateOrderCommand : ICommandRequest
{
    public int OrderId { get; set; }
}

public class CreateOrderHandler : ICommandHandler<CreateOrderCommand>
{
    public async Task Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Business logic for creating an order
    }
}
```

### **2. Queries**

Define a query using `IQueryRequest` and handle it with `IQueryHandler`:

```csharp
using Franz.Common.Business.Queries;

public class GetOrderQuery : IQueryRequest<Order>
{
    public int OrderId { get; set; }
}

public class GetOrderHandler : IQueryHandler<GetOrderQuery, Order>
{
    public async Task<Order> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        // Fetch the order based on OrderId
        return new Order();
    }
}
```

### **3. Domain-Driven Design**

Use core abstractions for domain models:
- Define an aggregate root:

```csharp
using Franz.Common.Business.Domain;

public class Order : AggregateRoot
{
    public int OrderId { get; private set; }
    public string CustomerName { get; private set; }

    public Order(int orderId, string customerName)
    {
        OrderId = orderId;
        CustomerName = customerName;
    }
}
```

- Implement a repository for aggregates:

```csharp
using Franz.Common.Business.Domain;

public class OrderRepository : IAggregateRepository<Order>
{
    // Implement repository methods
}
```

### **4. Events**

Publish and handle events using the `IEvent` and `IEventHandler` interfaces:

```csharp
using Franz.Common.Business.Events;

public class OrderCreatedEvent : IEvent
{
    public int OrderId { get; set; }
}

public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        // Handle the event
    }
}
```

### **5. Dependency Injection**

Register all handlers and services using `ServiceCollectionExtensions`:

```csharp
using Franz.Common.Business.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddBusinessServices(); // Registers commands, queries, and event handlers automatically
    }
}
```

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team. If you have access, follow these steps:
1. Clone the repository.
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.2.62
- Added abstractions for commands, queries, and events.
- Introduced DDD components: `AggregateRoot`, `Entity`, `ValueObject`.
- Integrated MediatR for CQRS and event handling.
- Added `ServiceCollectionExtensions` for streamlined dependency registration.

