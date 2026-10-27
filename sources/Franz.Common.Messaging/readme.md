# Franz.Common.Messaging

> Messaging and distributed integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Messaging` package provides messaging and distributed integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Messaging
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

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

* **Current Version:** v2.3.2
* Part of the private **Franz Framework** ecosystem.

---

## 📜 License

This library is licensed under the MIT License. See the `LICENSE` file for details.




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

### Version 1.6.20
- Updated to **.NET 10.0**


### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

The messaging core dependency references were reviewed as part of the repository-wide .NET 10 servicing update. The intended public messaging contracts remain unchanged. Verify all transport, serialization, and persistence integration tests against the resolved dependency graph before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
