Got it ✅ — let’s update the **README for Franz.Common.MongoDB v1.5.10** to reflect all the new changes we introduced (Inbox store, Outbox store, DLQ, indexes, DI extensions, etc.).

Here’s the rewritten version:

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

* **Current Version**: 1.5.10
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

### Version 1.5.10

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

```

---

