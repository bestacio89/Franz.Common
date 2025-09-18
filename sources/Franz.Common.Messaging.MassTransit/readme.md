# **Franz.Common.Messaging.MassTransit**

A library within the **Franz Framework** that integrates **MassTransit** with messaging workflows. This package simplifies the setup and management of Kafka-based messaging using **MassTransit**, providing tools for producers, consumers, and client registrations.

---

## **Features**

- **MassTransit Integration**:
  - Extends **MassTransit** functionality to Kafka-based workflows.
- **Kafka Messaging**:
  - Interfaces for Kafka producers (`IKafkaProducer`) and consumers (`IKafkaConsumer`).
- **Messaging Client**:
  - `IMessagingClient` for abstracting and managing messaging operations.
- **Service Registration**:
  - `ServiceRegistration` to streamline the setup of Kafka producers and consumers.

---

## **Version Information**

- - **Current Version**: 1.3.13
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **MassTransit** (8.3.4): Provides in-process messaging and event-driven architecture tools.
- **MassTransit.Kafka** (8.3.4): Adds Kafka transport support for MassTransit.

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
dotnet add package Franz.Common.Messaging.MassTransit  
```

---

## **Usage**

### **1. Register Kafka Messaging with MassTransit**

Use `ServiceRegistration` to set up Kafka producers and consumers:

```csharp
using Franz.Common.Messaging.MassTransit.Extensions;

services.AddMassTransitKafkaMessaging(cfg =>
{
    cfg.Host("localhost:9092", h =>
    {
        h.Username = "your-username";
        h.Password = "your-password";
    });
});
```

### **2. Implement Kafka Producers and Consumers**

Define Kafka producer and consumer interfaces:

```csharp
using Franz.Common.Messaging.MassTransit.Contracts;

public interface IOrderCreatedProducer : IKafkaProducer<OrderCreatedEvent> { }
public interface IOrderCreatedConsumer : IKafkaConsumer<OrderCreatedEvent> { }
```

Implement the producer and consumer logic:

```csharp
using Franz.Common.Messaging.MassTransit.Clients;

public class OrderCreatedProducer : KafkaProducer<OrderCreatedEvent>, IOrderCreatedProducer
{
    public OrderCreatedProducer(IMessagingClient client) : base(client) { }
}

public class OrderCreatedConsumer : KafkaConsumer<OrderCreatedEvent>, IOrderCreatedConsumer
{
    public OrderCreatedConsumer(IMessagingClient client) : base(client) { }
}
```

### **3. Use Messaging Clients**

Leverage `IMessagingClient` to abstract messaging operations:

```csharp
using Franz.Common.Messaging.MassTransit.Clients;

public class MessagingService
{
    private readonly IMessagingClient _messagingClient;

    public MessagingService(IMessagingClient messagingClient)
    {
        _messagingClient = messagingClient;
    }

    public async Task PublishMessageAsync<T>(T message) where T : class
    {
        await _messagingClient.PublishAsync(message);
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.MassTransit** package integrates seamlessly with:
- **MassTransit**: Enables robust event-driven messaging workflows.
- **MassTransit.Kafka**: Adds Kafka transport support for MassTransit.
- **Franz Framework**: Extends Kafka messaging capabilities across distributed systems.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team. If you have access, follow these steps:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.2.65
- Added `IKafkaProducer` and `IKafkaConsumer` interfaces for Kafka operations.
- Integrated `IMessagingClient` for abstracted messaging operations.
- Provided `ServiceRegistration` for streamlined Kafka setup with MassTransit.
- Full compatibility with **MassTransit** and **MassTransit.Kafka**.


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**