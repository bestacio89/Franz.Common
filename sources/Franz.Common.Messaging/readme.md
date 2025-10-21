
# Franz.Common.Messaging

A messaging abstraction library within the **Franz Framework** that provides a unified foundation for building reliable, resilient, and extensible distributed messaging systems.  

It supports **outbox**, **inbox**, **retries**, **dead-letter queues**, and **multiple transports** (starting with **Kafka** and **MongoDB**).

---

## ✨ Features

- **📦 Outbox Pattern**
  - Reliable delivery with `OutboxPublisherService`.
  - Retries with exponential backoff.
  - Moves failed messages to a **Dead Letter Queue (DLQ)** after max retries.
  - MongoDB-backed implementation (`MongoMessageStore`).

- **📥 Inbox Pattern**
  - Prevents duplicate processing with `IInboxStore`.
  - MongoDB-backed implementation (`MongoInboxStore`).
  - Guarantees **idempotency** under retries or replays.

- **🧩 Serializer Abstraction**
  - Unified `IMessageSerializer` contract.
  - Default `JsonMessageSerializer` (camelCase, ignore nulls).
  - Shared across Kafka, Mongo, and Outbox.

- **📊 Observability & Monitoring**
  - Structured logging (with emojis ✅⚠️🔥).
  - OpenTelemetry-friendly hooks.
  - Covers retries, DLQ moves, dispatch, and consumption.

- **🎧 Listeners & Hosting**
  - Transport-agnostic `IListener` interface.
  - Dedicated listeners:
    - `KafkaListener` (transport-only)
    - `OutboxListener` (transport-only)
  - Hosted service wrappers:
    - `KafkaHostedService`
    - `OutboxHostedService`
  - Clean **separation of transport vs hosting concerns**.

- **⚡ MongoDB Integration**
  - `MongoMessageStore` with automatic index creation:
    - `SentOn`
    - `RetryCount`
    - `CreatedOn`
  - `MongoInboxStore` with unique index on message IDs.
  - DI extensions: `AddMongoMessageStore`, `AddMongoInboxStore`.

---

## 📂 Project Structure

```

Franz.Common.Messaging/
├── Configuration/
├── Contexting/
├── Delegating/
├── Extensions/
├── Factories/
├── Headers/
├── Outboxes/
│    ├── OutboxOptions.cs
│    ├── OutboxPublisherService.cs
│    └── ServiceCollectionExtensions.cs
├── Serialization/
│    ├── ISerializer.cs
│    ├── JsonMessageSerializer.cs
│    └── ServiceCollectionExtensions.cs
├── Storage/
│    ├── InboxStore.cs
│    ├── IMessageStore.cs
│    ├── StoredMessage.cs
│    ├── Mappings/MessageMappingExtensions.cs
│    └── …
├── Message.cs
├── IMessageSender.cs
└── …

````

Hosting-specific projects:
- **Franz.Common.Messaging.Hosting** → defines `IListener`, `MessageContext`, base services.
- **Franz.Common.Messaging.Hosting.Kafka** → `KafkaHostedService`.
- **Franz.Common.Messaging.Hosting.Mongo** → `OutboxHostedService`, `InboxHostedService`.

---

## ⚙️ Configuration

`MessagingOptions` in `appsettings.json`:

```json
"Messaging": {
  "BootstrapServers": "localhost:9092",
  "GroupId": "my-service",
  "OutboxCollection": "OutboxMessages",
  "DeadLetterCollection": "DeadLetterMessages",
  "InboxCollection": "InboxMessages"
}
````

---

## ⚡ Dependency Injection Setup

```csharp
builder.Services.AddMessagingCore();
builder.Services.AddMongoMessageStore(configuration);
builder.Services.AddMongoInboxStore(configuration);
builder.Services.AddKafkaHostedListener();
builder.Services.AddOutboxHostedListener();
```

---

## 🔄 Typical Flow

1. **Send Command/Event** → via `IMessagingSender`.
2. **Persist in Outbox** → `MongoMessageStore`.
3. **Publisher Service** → retries + DLQ if needed.
4. **Transport** → Kafka.
5. **Listener** → consumes message.
6. **Inbox Check** → skip if already processed.
7. **Dispatcher** → `SendAsync` (command) / `PublishAsync` (event).

---

## 🚀 Extensibility

* Add new transports (RabbitMQ, Azure Service Bus, etc.):

  * Implement `IListener` + HostedService in `Hosting.[Transport]`.
  * Add DI registration extensions.
* Swap Mongo for SQL by implementing `IMessageStore` and `IInboxStore`.
* Replace JSON with custom serializers via `IMessageSerializer`.

---

## 📊 Observability

* Emoji-style structured logs for clarity:

  * ✅ Success
  * ⚠️ Retry
  * 🔁 Skipped (Inbox)
  * 🔥 Dead Letter
* Compatible with OpenTelemetry for tracing message lifecycles.

---

## 📌 Roadmap

* Batch consumption support.
* Message expiration / cleanup.
* RabbitMQ transport (`Franz.Common.Messaging.Hosting.RabbitMq`).

---

## 📝 Version Information

* **Current Version**: 1.6.16
* Part of the private **Franz Framework** ecosystem.

---

## 📜 License

This library is licensed under the MIT License. See the `LICENSE` file for details.

```



## **Changelog**

### Version 1.2.65
- Upgrade version to .net 9


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.3.6
- Integrated with Franz.Mediator (no MediatR).
- MessagingPublisher.Publish is now async Task.
- MessagingInitializer scans INotificationHandler<> for events.
- Kafka topics auto-created for all integration events.
