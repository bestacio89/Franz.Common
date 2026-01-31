# Franz.Common.Messaging.Kafka

A Kafka transport integration for the **Franz Framework**, designed to provide
clean, deterministic, and production-grade interaction with Kafka topics,
producers, and consumers.

This package focuses **exclusively on Kafka transport concerns**:
configuration, producers, consumers, serialization, and transactions.

> 🧱 Hosting, background execution, and listeners are intentionally **not**
> handled here. They live in **Franz.Common.Messaging.Hosting.Kafka**.

---

## ✨ Features

### Kafka Transport & Configuration
- Centralized Kafka configuration via `MessagingOptions`
- `ConnectionProvider` and `ConnectionFactoryProvider` for managing Kafka connections
- Deterministic, DI-friendly Kafka setup

### Kafka Producers
- `MessagingPublisher` for publishing integration events
- `MessagingSender` for point-to-point messaging
- Automatic topic resolution and naming strategies

### Kafka Consumers
- Native usage of `Confluent.Kafka.IConsumer<string, string>`
- `IKafkaConsumerFactory` as the single authority for consumer creation
- Correct lifetime management aligned with Kafka consumer group semantics

### Modeling
- `KafkaModel` and `ModelProvider` for Kafka-based domain modeling
- Strong separation between messaging models and business logic

### Serialization
- `IMessageDeserializer`
- `JsonMessageDeserializer`
- Deterministic JSON serialization using Franz messaging contracts

### Transactions
- `MessagingTransaction` for Kafka-backed transactional workflows

### Utilities
- `ExchangeNamer` and `TopicNamer` for consistent topic naming
- Messaging helpers shared across Franz transports

### Dependency Injection
- Fluent `ServiceCollectionExtensions` for Kafka transport registration
- No accidental hosting or background execution side effects

---

## 🧭 Architectural Scope

This package is **transport-only**.

| Responsibility | Package |
|----------------|--------|
| Kafka producers | ✅ Franz.Common.Messaging.Kafka |
| Kafka consumers (transport) | ✅ Franz.Common.Messaging.Kafka |
| Background listeners | ❌ |
| Hosted services | ❌ |
| Message dispatch pipelines | ❌ |
| Hosting / workers | ➜ Franz.Common.Messaging.Hosting.Kafka |

This separation ensures:
- Testability with Testcontainers
- Clean CI/CD pipelines
- No hidden threads or background services
- Reuse in CLI tools, workers, APIs, and serverless contexts

---

## 📦 Dependencies

This package depends on:

- **Confluent.Kafka** (2.3.0)  
  Core Kafka client implementation

- **Franz.Common.Messaging**  
  Core messaging abstractions and contracts

- **Franz.Common.Annotations**  
  Messaging and modeling annotations

> ⚠️ Hosting integration is intentionally excluded.
> Use **Franz.Common.Messaging.Hosting.Kafka** for background listeners.

---

## 📥 Installation

### From NuGet

```bash
dotnet add package Franz.Common.Messaging.Kafka
````

---

## 🚀 Usage

### 1️⃣ Register Kafka Transport

```csharp
using Franz.Common.Messaging.Kafka.Extensions;

public void ConfigureServices(IServiceCollection services)
{
    services.AddKafkaMessaging(configuration);
}
```

This registers:

* Kafka producers
* Kafka senders
* Kafka consumers (transport only)

No hosted services are started.

---

### 2️⃣ Publish Messages

```csharp
public class OrderPublisher
{
    private readonly IMessagingPublisher _publisher;

    public OrderPublisher(IMessagingPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishAsync(OrderCreatedEvent evt)
    {
        await _publisher.PublishAsync(evt);
    }
}
```

---

### 3️⃣ Send Messages

```csharp
public class PaymentSender
{
    private readonly IMessagingSender _sender;

    public PaymentSender(IMessagingSender sender)
    {
        _sender = sender;
    }

    public async Task SendAsync(PaymentCommand command)
    {
        await _sender.SendAsync(command);
    }
}
```

---

### 4️⃣ Kafka Consumers (Important)

Franz **does not re-abstract Kafka consumers**.

Consumers are provided **directly** by Confluent:

```csharp
IConsumer<string, string>
```

They are:

* Created by `IKafkaConsumerFactory`
* Registered as long-lived singletons
* Fully compatible with Confluent.Kafka tooling and documentation

This avoids:

* Interface drift
* Partial re-implementations
* Subtle incompatibilities during upgrades

---

## 🧪 Testing & CI

This design is fully compatible with:

* Testcontainers (Kafka)
* Azure DevOps pipelines
* Docker-based integration tests
* Local developer environments

Because:

* No background services auto-start
* No implicit threads are created
* Kafka consumers are explicit and controlled

---

## 🔗 Integration with the Franz Framework

This package integrates with:

* **Franz.Common.Messaging**
* **Franz.Common.Messaging.Hosting.Kafka** (optional, for background execution)
* **Franz.Common.Mediator**
* **Franz.Common.EntityFramework**
* **Franz.Common.Business**

Kafka is treated as a **transport**, not an execution model.

---

## 🧾 Versioning & Changelog

### **Current Version**: 1.7.7

### **Version 1.7.01**

* 🧱 Corrected transport vs hosting separation
* 🔌 Removed Kafka consumer re-abstraction
* 🏭 Centralized consumer creation via `IKafkaConsumerFactory`
* ♻️ Fixed DI lifetimes for Kafka consumers
* 🧩 Clean separation between messaging and hosting layers
* 🧪 Improved Testcontainers and CI reliability

---

## 📄 License

MIT License
See the `LICENSE` file for details.

---

## 🧠 Final Note

This package intentionally mirrors **enterprise messaging frameworks**
(MassTransit, NServiceBus, Brighter) by enforcing:

> **Transport ≠ Hosting**

That separation is what makes Franz:

* Predictable
* Testable
* Scalable
* Production-safe
