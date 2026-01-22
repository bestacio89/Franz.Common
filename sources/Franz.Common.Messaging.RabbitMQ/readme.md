# Franz.Common.Messaging.RabbitMQ

Franz now speaks **RabbitMQ**.  
The same architectural contract used for Kafka applies here — **one pipeline, one model, zero spaghetti**.

RabbitMQ is just a transport. Franz does the rest.

---

## 🚀 Getting Started

Install the package:

```bash
dotnet add package Franz.Common.Messaging.RabbitMQ
```

Register RabbitMQ messaging:

```csharp
using Franz.Common.Messaging.RabbitMQ.Extensions;

public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddRabbitMQMessaging(configuration);
}
```

---

## 📦 Features

- **Publisher & Sender** via DI (`IMessagingPublisher`, `IMessagingSender`)
- **Consumers** as hosted listeners
- **Inbox / Outbox** support (MongoDB-backed)
- **Transactional publishing** (`IMessagingTransaction`)
- **Replay strategies** (default + extensible)
- **Message context accessor** for scoped metadata
- **Enforced lifetimes** to prevent invalid wiring
- **Duplicate-safe DI registration**

Kafka and RabbitMQ share the **exact same messaging semantics** in Franz.

---

## ⚖️ Philosophy

> *“We don’t care about MQ.  
> We make it behave.”*

Franz removes broker-specific complexity.  
Choose **Kafka** or **RabbitMQ** — your application code does not change.

---

**Current Version**: 1.7.6

---

## 🆕 v1.7.2 — Stability & Infrastructure Hardening

This release finalizes RabbitMQ as a **first-class transport** in Franz.

### ✔ Highlights

- Fully wired **RabbitMQ infrastructure** (publisher, consumers, hosted services)
- **Inbox & Outbox** validated with real MongoDB containers
- RabbitMQ now behaves **identically to Kafka** at the abstraction level

### 🔧 Improvements

- Correct **RabbitMQ.Client 7.x async channel handling**
- Closed all **DI wiring gaps** in hosted services and tests
- All **message builder strategies** registered by default
- Deterministic startup / shutdown behavior

### 🧪 Reliability

- Tested against **real RabbitMQ + MongoDB** using Testcontainers
- No mocks. No shortcuts. Real brokers, real persistence.

> **Result:** RabbitMQ is production-ready and battle-tested.

---

### v1.6.20

- Migration to **.NET 10**
- Improved messaging DI patterns
- Updated RabbitMQ client
- Alignment of messaging abstractions across transports
