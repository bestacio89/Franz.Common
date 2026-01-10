# Franz.Common.Messaging.Hosting.Kafka

A **Kafka hosting integration** for the **Franz Framework** that connects the Kafka
transport layer (`Franz.Common.Messaging.Kafka`) to the .NET hosting runtime
(`Microsoft.Extensions.Hosting`).

This package is responsible **only** for:
- background execution
- hosted listeners
- message dispatch orchestration

> 🧱 Kafka transport, producers, consumers, and configuration live in  
> **Franz.Common.Messaging.Kafka**

---

## ✨ Features

### Hosted Services
- **`KafkaHostedService`**  
  Runs a Kafka listener inside a .NET `BackgroundService`.

- **`MessagingHostedService`**  
  Orchestrates message dispatch:
  - creates scoped execution contexts
  - resolves messaging strategies
  - dispatches messages safely

- **`OutboxHostedService`**  
  Publishes messages stored in an outbox (SQL / Mongo) to Kafka reliably.

### Dependency Injection Extensions
- `KafkaHostingServiceCollectionExtensions`
- Simple registration via:
  - `AddKafkaHostedListener()`
  - `AddOutboxHostedListener()`

### Separation of Concerns
- **No Kafka configuration here**
- **No Kafka consumer creation here**
- Hosting depends on abstractions only

### Observability
- Structured logging
- Emoji-based lifecycle signals
- Compatible with OpenTelemetry
- Correlation via `MessageContextAccessor`

---

## 📂 Project Structure

```text
Franz.Common.Messaging.Hosting.Kafka/
├── Extensions/
│   └── KafkaHostingServiceCollectionExtensions.cs
├── HostedServices/
│   ├── KafkaHostedService.cs
│   ├── MessagingHostedService.cs
│   └── OutboxHostedService.cs
└── README.md
````

---

## ⚙️ Dependencies

* **Microsoft.Extensions.Hosting** (10.0.0)
* **Microsoft.Extensions.DependencyInjection.Abstractions** (10.0.0)
* **Franz.Common.Messaging**
* **Franz.Common.Messaging.Kafka**
* **Franz.Common.Messaging.Hosting**

> ⚠️ This package assumes Kafka transport is already registered via
> `Franz.Common.Messaging.Kafka`.

---

## 🚀 Usage

### 1️⃣ Register Kafka Transport (required)

```csharp
using Franz.Common.Messaging.Kafka.Extensions;

services.AddKafkaMessaging(configuration);
```

---

### 2️⃣ Register Kafka Hosted Services

```csharp
using Franz.Common.Messaging.Hosting.Kafka.Extensions;

services.AddKafkaHostedListener(configuration);
services.AddOutboxHostedListener(configuration);
```

This will:

* start background Kafka consumption
* dispatch messages through Franz messaging strategies
* process outbox messages reliably

---

## 🔄 Kafka Hosted Service Lifecycle

```csharp
public sealed class KafkaHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribes to Kafka topics
        // Listens until host shutdown
        // Dispatches messages safely
    }
}
```

Behavior:

* graceful shutdown
* no host crashes on bad messages
* scope-per-message execution

---

## 📦 Outbox Hosted Service

```csharp
public sealed class OutboxHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Reads outbox
        // Publishes to Kafka
        // Handles retries & DLQ
    }
}
```

Guarantees:

* at-least-once delivery
* retry control
* no message loss on crashes

---

## 📊 Observability

* Emoji logging conventions:

  * 🚀 startup
  * 🛑 shutdown
  * ⚠️ recoverable failure
  * ❌ execution error
  * 🔥 DLQ
* Correlation IDs via `MessageContextAccessor`
* OpenTelemetry-compatible spans

---

## 🧭 Architectural Role

| Concern                 | Package                              |
| ----------------------- | ------------------------------------ |
| Kafka transport         | Franz.Common.Messaging.Kafka         |
| Kafka consumer creation | Franz.Common.Messaging.Kafka         |
| Hosted execution        | Franz.Common.Messaging.Hosting.Kafka |
| Dispatch orchestration  | Franz.Common.Messaging.Hosting.Kafka |

This ensures:

* clean Testcontainers integration
* deterministic CI behavior
* no hidden threads
* safe message handling

---

## 🧾 Version Information

* **Current Version**: 1.7.5
* **Target Framework**: **.NET 10.0**
* Part of the **Franz Framework**

---

## 📖 Changelog

### **Version 1.7.01**

* 🧱 Enforced strict separation between Kafka transport and hosting
* 🔌 Removed Kafka consumer creation from hosting layer
* 🔄 Hosting now depends exclusively on `IListener`
* ♻️ Fixed lifecycle handling and graceful shutdown
* 🧪 Improved Testcontainers and CI reliability
* ⬆️ Upgraded to **.NET 10.0**

---

### Version 1.6.20

* Initial Kafka hosting support
* Added `KafkaHostedService`
* Added `OutboxHostedService`
* Introduced hosting DI extensions
* Integrated `MessageContextAccessor`
* Emoji-based logging conventions

---

## 📜 License

MIT License
See the `LICENSE` file for details.



