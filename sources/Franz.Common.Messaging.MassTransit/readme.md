# Franz.Common.Messaging.MassTransit

> MassTransit transport adapter for publishing and consuming Franz.Common message contracts.

## Overview

MassTransit transport adapter for publishing and consuming Franz.Common message contracts.

## Installation

```bash
dotnet add package Franz.Common.Messaging.MassTransit
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A library within the **Franz Framework** that integrates **MassTransit** with messaging workflows. This package simplifies the setup and management of Kafka-based messaging using **MassTransit**, providing tools for producers, consumers, and client registrations.

---

## **Features**

- **MassTransit Integration**:
  - Extends **MassTransit** functionality to Kafka-based workflows.
- **Kafka Messaging**:
  - Interfaces for Kafka producers (`IKafkaProducer`) and consumers (`IKafkaConsumer`).
- **Service Registration**:
  - `ServiceRegistration` to streamline the setup of Kafka producers and consumers.

---

## **Version Information**

**Current Version:** v3.0.0
- Part of the open-source Franz.Common ecosystem.

---

## **Dependencies**

This package relies on:
- **MassTransit** (8.3.4): Provides in-process messaging and event-driven architecture tools.
- **MassTransit.Kafka** (8.3.4): Adds Kafka transport support for MassTransit.

---

## **Installation**

### Install from NuGet.org

```bash
dotnet add package Franz.Common.Messaging.MassTransit
```

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

public interface IOrderCreatedProducer : IKafkaProducer { }
public interface IOrderCreatedConsumer : IKafkaConsumer { }
```

Use the role-specific contracts directly. Producers expose publishing only; consumers expose scoped subscription only.

```csharp
public sealed class OrderPublisher(IKafkaProducer producer)
{
    public Task PublishAsync(OrderCreatedEvent message, CancellationToken ct)
        => producer.PublishAsync(message, "orders.created", ct);
}

public sealed class OrderConsumer(IKafkaConsumer consumer)
{
    public Task StartAsync(CancellationToken ct)
        => consumer.SubscribeAsync<OrderCreatedEvent>(
            "orders.created",
            async (services, message) =>
            {
                var handler = services.GetRequiredService<IOrderCreatedHandler>();
                await handler.HandleAsync(message, ct);
            },
            ct);
}
```

There is no combined `IMessagingClient` abstraction and no unscoped captured-handler overload. Each consumer delivery is executed through a fresh dependency-injection scope.

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.MassTransit** package integrates seamlessly with:
- **MassTransit**: Enables robust event-driven messaging workflows.
- **MassTransit.Kafka**: Adds Kafka transport support for MassTransit.
- **Franz Framework**: Extends Kafka messaging capabilities across distributed systems.

---

## **Contributing**

Contributions are welcome under the MIT License. To propose a change:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request for review.

---

## License

Licensed under the MIT License. See the repository [LICENSE](../../LICENSE) for details.

## **Changelog**

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- Role-specific producer/consumer contracts; scoped consumer handlers only.
- Tests, listeners, and pipeline components modernized.

### Version 1.2.65
- Added `IKafkaProducer` and `IKafkaConsumer` interfaces for Kafka operations.
- Removed the combined `IMessagingClient`; producer and consumer contracts are independent.
- Provided `ServiceRegistration` for streamlined Kafka setup with MassTransit.
- Full compatibility with **MassTransit** and **MassTransit.Kafka**.


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**


## Tenant-aware Kafka handlers in 2.3.4

When tenant-aware processing is enabled, prefer the scoped-handler overload so handler dependencies are resolved from the per-delivery service provider and cannot be accidentally captured across tenant executions. The unscoped captured-handler overload has been removed. Every consumer handler receives the per-delivery service provider and executes inside a fresh scope. Verify Kafka header mapping and tenant authorization in the configured consumer pipeline; header presence alone does not establish tenant membership. See the [messaging execution lifecycle](../../docs/multitenancy-execution-lifecycle.md).
