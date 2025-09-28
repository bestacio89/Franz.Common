# Franz.Common.Messaging.Hosting

A foundational library within the **Franz Framework** designed to enable and manage hosted messaging services in distributed applications.  
This package provides hosting utilities, message execution strategies, delegation, transaction management, and seamless integration with **Microsoft.Extensions.Hosting**.

---

## ✨ Features

- **Hosted Messaging Services**
  - `MessagingHostedService` for orchestrating background message processing.
  - Wrapper services for Kafka and Outbox listeners (`KafkaHostedService`, `OutboxHostedService`).

- **Message Context Management**
  - `MessageContextAccessor` manages per-message context during execution.
  - Async-local storage ensures safe context propagation across async flows.

- **Delegating Message Actions**
  - `IAsyncMessageActionFilter` and `MessageActionExecutionDelegate` let you extend/control pipelines before/after handling.

- **Transaction Management**
  - `TransactionFilter` ensures consistency across message processing and persistence.

- **Messaging Execution Strategies**
  - `IMessagingStrategyExecuter` defines custom strategies for message execution.

- **📥 Inbox Pattern Integration**
  - Built-in `IInboxStore` support for idempotent message consumption.
  - Ensures "exactly-once" processing even under retries or replays.

- **📦 Outbox Pattern Integration**
  - Hosted service wrappers for `OutboxMessageListener` to continuously publish pending messages.

- **Extensions**
  - `HostBuilderExtensions` for wiring messaging services into the host builder.
  - `ServiceCollectionExtensions` for DI registration.

- **Observability**
  - Structured logging with emoji-friendly conventions (✅ success, ⚠️ retries, 🔥 DLQ).
  - OpenTelemetry hooks for distributed tracing.

---

## 📂 Project Structure

```

Franz.Common.Messaging.Hosting/
├── Context/
│    └── MessageContextAccessor.cs
├── Delegating/
│    └── IAsyncMessageActionFilter.cs
├── Executing/
│    └── IMessagingStrategyExecuter.cs
├── HostedServices/
│    ├── MessagingHostedService.cs
│    ├── KafkaHostedService.cs
│    └── OutboxHostedService.cs
├── Transactions/
│    └── TransactionFilter.cs
└── Extensions/
└── HostBuilderExtensions.cs

````

---

## ⚙️ Dependencies

- **Microsoft.Extensions.DependencyInjection.Abstractions** (8.0.0)  
- **Microsoft.Extensions.Hosting** (8.0.0)  
- **Franz.Common.Hosting** – general hosting utilities  
- **Franz.Common.Logging** – centralized structured logging  
- **Franz.Common.Messaging** – core messaging abstractions  

---

## 🚀 Usage

### 1. Register Messaging Hosted Services

```csharp
using Franz.Common.Messaging.Hosting.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .AddMessagingHostedService()
    .AddKafkaHostedListener()
    .AddOutboxHostedListener()
    .Build();

await host.RunAsync();
````

### 2. Customize Messaging Actions

```csharp
public class CustomMessageActionFilter : IAsyncMessageActionFilter
{
    public async Task OnMessageExecutingAsync(MessageActionExecutingContext context)
    {
        Console.WriteLine("🚦 Before message execution");
    }

    public async Task OnMessageExecutedAsync(MessageActionExecutedContext context)
    {
        Console.WriteLine("✅ After message execution");
    }
}
```

### 3. Transaction Management

```csharp
services.AddControllers(options =>
{
    options.Filters.Add<TransactionFilter>();
});
```

### 4. Implement Messaging Strategies

```csharp
public class CustomMessagingStrategyExecuter : IMessagingStrategyExecuter
{
    public async Task ExecuteAsync(MessageContext context)
    {
        // Custom execution logic
    }
}
```

---

## 📊 Observability

* Emoji-based structured logs (✅ success, ⚠️ retry, 🔥 DLQ, 💤 idle).
* Tracing via OpenTelemetry spans for Kafka + Outbox messages.

---

## 📝 Version Information

* **Current Version**: 1.5.10
* Part of the private **Franz Framework** ecosystem.
---

## 📜 License

This library is licensed under the MIT License. See the `LICENSE` file for details.

---

## 📖 Changelog

### Version 1.2.65

* Upgrade to .NET 9.

### Version 1.3

* Upgraded to **.NET 9.0.8**.
* Added new features and improvements.
* Separated business concepts from mediator concepts.
* Compatibility with both the in-house mediator and **MediatR**.

### Version 1.5.10

* Introduced **KafkaHostedService** and **OutboxHostedService** for clean separation of concerns.
* Added **Inbox pattern** integration (`IInboxStore`) with hosted listener support.
* Extended `MessageContextAccessor` with AsyncLocal for per-message scoping.
* Unified listener abstractions (`IListener`) with async cancellation token support.
* Introduced consistent DI extensions:

  * `AddKafkaHostedListener()`
  * `AddOutboxHostedListener()`
* Enhanced structured logging with emoji conventions for observability.
* Added OpenTelemetry hooks for distributed tracing of message processing.

```
