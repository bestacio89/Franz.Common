# Franz.Common.MongoDB

> Persistence and infrastructure integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.MongoDB` package provides persistence and infrastructure integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.MongoDB
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

---

```markdown
# Franz.Common.MongoDB

A library within the **Franz Framework** designed to streamline the integration of MongoDB into .NET applications.  
This package provides utilities for configuring MongoDB services, registering them with dependency injection, and enabling reliable **messaging persistence** patterns such as **Outbox** and **Inbox**.

---

## ✨ Features

- **MongoDB Service Registration**
  - `MongoServiceRegistration` for simplified client and database registration.
  - Options-driven configuration via `MongoOptions`.

- **📦 Outbox Support**
  - `MongoMessageStore` for storing pending messages.
  - Automatic retry and **Dead Letter Queue (DLQ)** support.
  - Auto-created indexes on:
    - `SentOn` (pending lookups)
    - `RetryCount` (retry cycles)
    - `CreatedOn` (cleanup/archival)

- **📥 Inbox Support**
  - `MongoInboxStore` ensures **idempotent message consumption**.
  - Unique index on processed message IDs prevents duplicates.

- **⚙️ Dependency Injection**
  - Extensions for clean setup:
    - `AddMongoMessageStore`
    - `AddMongoInboxStore`
    - `AddMongoDB`

- **🔒 Reliability**
  - Atomic updates for retries (`UpdateRetryAsync`).
  - Safe move to dead-letter collection (`MoveToDeadLetterAsync`).
  - Mark-as-sent functionality for outbox messages.

---

## 📂 Project Structure

```

Franz.Common.MongoDB/
├── Configuration/
│    └── MongoOptions.cs
├── Outbox/
│    ├── MongoMessageStore.cs
│    └── DeadLetterCollection.cs
├── Inbox/
│    └── MongoInboxStore.cs
├── Extensions/
│    └── ServiceCollectionExtensions.cs
└── MongoServiceRegistration.cs

````

---

## ⚙️ Configuration

`appsettings.json`:

```json
"Mongo": {
  "ConnectionString": "mongodb://localhost:27017",
  "DatabaseName": "MessagingDb",
  "OutboxCollection": "OutboxMessages",
  "DeadLetterCollection": "DeadLetterMessages",
  "InboxCollection": "InboxMessages"
}
````

---

## 🚀 Dependency Injection Setup

```csharp
builder.Services.AddMongoDB(options =>
{
    options.ConnectionString = "mongodb://localhost:27017";
    options.DatabaseName = "MessagingDb";
});

builder.Services.AddMongoMessageStore(configuration);
builder.Services.AddMongoInboxStore(configuration);
```

---

## 🔄 Usage

### Outbox Message Store

```csharp
var message = new StoredMessage
{
    Id = Guid.NewGuid().ToString(),
    Body = "{ \"OrderId\": 123 }",
    CreatedOn = DateTime.UtcNow
};

await _messageStore.SaveAsync(message);
```

### Inbox Message Store

```csharp
if (!await _inboxStore.HasProcessedAsync(message.Id))
{
    await _dispatcher.PublishAsync(orderCreatedEvent);
    await _inboxStore.MarkProcessedAsync(message.Id);
}
```

---

## 📊 Observability

* Mongo collections come with indexes for performance.
* Logs include retries ⚠️, DLQ moves 🔥, and successful sends ✅.
* OpenTelemetry hooks enabled via higher-level messaging layer.

---

## 📌 Roadmap

* TTL indexes for automatic cleanup of old outbox/inbox entries.
* Configurable sharding/replica set support for large-scale deployments.
* RabbitMQ-backed inbox/outbox stores (parallel to Kafka).

---

## 📝 Version Information

* **Current Version:** v2.3.0
* Part of the private **Franz Framework** ecosystem.

---

## 📜 License

This library is licensed under the MIT License. See the `LICENSE` file for details.

---

## 📖 Changelog

### Version 1.2.65

* Added `MongoServiceRegistration` for streamlined MongoDB service configuration and registration.
* Introduced support for custom MongoDB configurations.

### Version 1.3

* Upgraded to **.NET 9.0.8**.
* Added **new features and improvements**.
* Separated **business concepts** from **mediator concepts**.
* Now compatible with both the **in-house mediator** and **MediatR**.

### Version 1.6.2

* Introduced **Mongo Outbox** (`MongoMessageStore`) with retries and Dead Letter Queue.
* Added **Mongo Inbox** (`MongoInboxStore`) for idempotent consumption.
* Auto-created indexes on `SentOn`, `RetryCount`, and `CreatedOn`.
* Added dependency injection extensions:

  * `AddMongoMessageStore`
  * `AddMongoInboxStore`
* Extended `IMessageStore` with:

  * `UpdateRetryAsync`
  * `MoveToDeadLetterAsync`
  * `MarkAsSentAsync`
* Improved observability with structured logging and OpenTelemetry hooks.

---

### Version 1.6.20
- Updated to **.NET 10.0**

---

---

## Dependency maintenance — October 2026

The MongoDB driver was upgraded as part of the .NET 10 dependency baseline. Validate serialization, compression, and persistence integration tests against the resolved driver/transitive package graph before publishing. See [the repository dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
