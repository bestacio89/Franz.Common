
# Franz.Common.Messaging.Hosting.Kafka

A dedicated hosting library within the **Franz Framework** that provides **Kafka-specific hosted services** and **dependency injection extensions**.  
This package bridges the Kafka transport layer (`Franz.Common.Messaging.Kafka`) with the .NET hosting infrastructure (`Microsoft.Extensions.Hosting`).

---

## ✨ Features

- **Hosted Services**
  - `KafkaHostedService` – continuously consumes Kafka messages and dispatches them.
  - `OutboxHostedService` – publishes stored outbox messages to Kafka in the background.
  - `MessagingHostedService` – general-purpose hosted message orchestrator.

- **Dependency Injection Extensions**
  - `KafkaHostingServiceCollectionExtensions` simplifies service registration in `Startup`/`Program.cs`.
  - Provides one-liners like `AddKafkaHostedListener()` and `AddOutboxHostedListener()`.

- **Separation of Concerns**
  - Keeps transport logic (`Franz.Common.Messaging.Kafka`) separate from hosting concerns.
  - Makes testing listeners independent of the hosting runtime.

- **Observability**
  - Structured logging with emoji conventions (✅ success, ⚠️ retries, 🔥 DLQ).
  - Compatible with OpenTelemetry for distributed tracing.

---

## 📂 Project Structure

```

Franz.Common.Messaging.Hosting.Kafka/
├── Extensions/
│    └── KafkaHostingServiceCollectionExtensions.cs
├── HostedServices/
│    ├── KafkaHostedService.cs
│    ├── MessagingHostedService.cs
│    └── OutboxHostedService.cs
└── readme.md

````

---

## ⚙️ Dependencies

- **Microsoft.Extensions.Hosting** (8.0.0)  
- **Microsoft.Extensions.DependencyInjection.Abstractions** (8.0.0)  
- **Franz.Common.Messaging** – core messaging abstractions  
- **Franz.Common.Messaging.Kafka** – Kafka transport adapter  
- **Franz.Common.Messaging.Hosting** – base hosting abstractions  

---

## 🚀 Usage

### 1. Register Kafka Hosted Services

In `Program.cs` or `Startup.cs`:

```csharp
using Franz.Common.Messaging.Hosting.Kafka.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddKafkaHostedListener(context.Configuration);
        services.AddOutboxHostedListener(context.Configuration);
    })
    .Build();

await host.RunAsync();
````

### 2. Kafka Hosted Service

Runs in the background to consume Kafka messages and dispatch them via the mediator:

```csharp
public class KafkaHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consumes Kafka messages and dispatches
    }
}
```

### 3. Outbox Hosted Service

Ensures pending messages in MongoDB/SQL outbox are published to Kafka reliably:

```csharp
public class OutboxHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Reads outbox, sends to Kafka, handles retries/DLQ
    }
}
```

---

## 📊 Observability

* Emoji logging (✅ processed, ⚠️ retry, 🔥 DLQ, 💤 idle).
* Integrated with `MessageContextAccessor` for correlation IDs.
* Tracing compatible with **OpenTelemetry**.

---

## 📝 Version Information

* **Current Version**: 1.6.20
* Part of the private **Franz Framework** ecosystem.

---

## 📜 License

This library is licensed under the MIT License. See the `LICENSE` file for details.

---

## 📖 Changelog

### Version 1.6.2

* Introduced `KafkaHostedService` to run Kafka listeners inside .NET host.
* Added `OutboxHostedService` to bridge Mongo outbox with Kafka publishing.
* Added `KafkaHostingServiceCollectionExtensions` for simple DI registration.
* Unified hosted services with `MessageContextAccessor` and inbox idempotency support.
* Improved logging with emoji conventions and OpenTelemetry hooks.

### Version 1.6.20
- Updated to **.NET 10.0**