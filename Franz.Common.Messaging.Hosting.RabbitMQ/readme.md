# Franz.Common.Messaging.Hosting.RabbitMQ

A dedicated hosting library within the **Franz Framework** that provides **RabbitMQ-specific hosted services** and **dependency injection extensions**.
This package bridges the RabbitMQ transport layer (`Franz.Common.Messaging.RabbitMQ`) with the .NET hosting infrastructure (`Microsoft.Extensions.Hosting`).

---

## ✨ Features

* **Hosted Services**

  * `RabbitMQHostedService` – continuously consumes RabbitMQ messages and dispatches them.
  * `OutboxHostedService` – publishes stored outbox messages to RabbitMQ in the background.
  * `MessagingHostedService` – general-purpose hosted message orchestrator.

* **Dependency Injection Extensions**

  * `RabbitMQHostingServiceCollectionExtensions` simplifies service registration in `Startup`/`Program.cs`.
  * Provides one-liners like `AddRabbitMQHostedListener()` and `AddOutboxHostedListener()`.

* **Separation of Concerns**

  * Keeps transport logic (`Franz.Common.Messaging.RabbitMQ`) separate from hosting concerns.
  * Makes testing listeners independent of the hosting runtime.

* **Observability**

  * Structured logging with emoji conventions (✅ success, ⚠️ retries, 🔥 DLQ).
  * Compatible with OpenTelemetry for distributed tracing.

---

## 📂 Project Structure

```
Franz.Common.Messaging.Hosting.RabbitMQ/
├── Extensions/
│    └── RabbitMQHostingServiceCollectionExtensions.cs
├── HostedServices/
│    ├── RabbitMQHostedService.cs
│    ├── MessagingHostedService.cs
│    └── OutboxHostedService.cs
└── readme.md
```

---

## ⚙️ Dependencies

* **Microsoft.Extensions.Hosting** (8.0.0)
* **Microsoft.Extensions.DependencyInjection.Abstractions** (8.0.0)
* **Franz.Common.Messaging** – core messaging abstractions
* **Franz.Common.Messaging.RabbitMQ** – RabbitMQ transport adapter
* **Franz.Common.Messaging.Hosting** – base hosting abstractions

---

## 🚀 Usage

### 1. Register RabbitMQ Hosted Services

In `Program.cs` or `Startup.cs`:

```csharp
using Franz.Common.Messaging.Hosting.RabbitMQ.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddRabbitMQHostedListener(opts =>
        {
            opts.ConnectionString = context.Configuration["RabbitMQ:ConnectionString"];
            opts.ExchangeName = context.Configuration["RabbitMQ:ExchangeName"];
        });

        services.AddOutboxHostedListener(opts =>
        {
            opts.OutboxTable = context.Configuration["Outbox:TableName"];
        });
    })
    .Build();

await host.RunAsync();
```

### 2. RabbitMQ Hosted Service

Runs in the background to consume RabbitMQ messages and dispatch them via the mediator:

```csharp
public class RabbitMQHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consumes RabbitMQ messages and dispatches
    }
}
```

### 3. Outbox Hosted Service

Ensures pending messages in MongoDB/SQL outbox are published to RabbitMQ reliably:

```csharp
public class OutboxHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Reads outbox, sends to RabbitMQ, handles retries/DLQ
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

* **Current Version**: 1.6.2
* Part of the private **Franz Framework** ecosystem.

---

## 📜 License

This library is licensed under the MIT License. See the `LICENSE` file for details.

---

## 📖 Changelog

### Version 1.6.2

* Introduced `RabbitMQHostedService` to run RabbitMQ listeners inside .NET host.
* Added `OutboxHostedService` to bridge Mongo/SQL outbox with RabbitMQ publishing.
* Added `RabbitMQHostingServiceCollectionExtensions` for simple DI registration.
* Unified hosted services with `MessageContextAccessor` and inbox idempotency support.
* Improved logging with emoji conventions and OpenTelemetry hooks.

