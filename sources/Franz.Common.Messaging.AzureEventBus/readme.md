# Franz.Common.Messaging.AzureEventBus

`Franz.Common.Messaging.AzureEventBus` is the **Azure Service Bus transport adapter** for the **Franz Framework messaging stack**.

It implements Franz messaging abstractions to provide **durable, reliable, and mediator-driven event delivery** using **Azure Service Bus Topics & Subscriptions**, while preserving all Franz guarantees:

* deterministic metadata
* mediator pipelines
* structured logging
* retry & dead-letter semantics
* transport isolation

This package is the **Azure equivalent** of:

* `Franz.Common.Messaging.Kafka`
* `Franz.Common.Messaging.RabbitMQ`
### 🟦 Azure Service Bus Transport

* Native integration with **Azure Service Bus**
* Topic / Subscription model
* Queue support (for commands, if enabled)
* Azure-native retry & DLQ semantics

---

### 🧠 Franz-Native Semantics

* Uses **Franz.Common.Messaging** abstractions
* Dispatches messages through **Franz.Common.Mediator**
* Fully compatible with Franz outbox / inbox patterns
* Deterministic correlation & causation propagation

---

### 🧩 Explicit Mapping Layer (No AutoMapper)

* Uses **Franz.Common.Mapping**
* Single authoritative translation layer:

  * Franz message ⇄ Service Bus message
* No reflection magic
* Version-safe & auditable mappings

---

### 🔁 Retry & Dead Letter Handling

* Retries handled by **Azure Service Bus delivery counts**
* Explicit dead-letter routing for:

  * Deserialization failures
  * Validation errors
  * Poison messages
* Clear separation between **business failure** and **transport failure**

---

### 📊 Observability & Diagnostics

* Integrated with **Franz.Common.Logging**
* Structured logs with Franz conventions
* CorrelationId & MessageId propagation
* Compatible with OpenTelemetry (via mediator pipelines)

---

## 📦 Dependencies

This package intentionally depends only on **core Franz building blocks**:

```
Franz.Common.Messaging
Franz.Common.Mediator
Franz.Common.Mediator.Polly
Franz.Common.Logging
Franz.Common.Errors
Franz.Common.Headers
Franz.Common.Mapping
Franz.Common.Serialization
Azure.Messaging.ServiceBus
```

❌ No HTTP
❌ No hosting logic
❌ No business dependencies
❌ No AutoMapper
│   └── AzureEventBusOptions.cs
│
├── Constants/
│   └── AzureEventBusHeaders.cs
│
├── Producers/
│   └── AzureEventBusProducer.cs
│
├── Consumers/
│   ├── AzureEventBusConsumer.cs
│   └── AzureEventBusProcessor.cs
│
├── Mapping/
│   └── AzureEventBusMessageMapper.cs
│
├── Infrastructure/
│   ├── ServiceBusClientFactory.cs
│   ├── ServiceBusSenderFactory.cs
│   └── ServiceBusProcessorFactory.cs
│
├── DependencyInjection/
│   └── ServiceCollectionExtensions.cs
│
└── README.md
```
## ⚙️ Configuration
```csharp
services.AddFranzAzureEventBus(options =>
{
    options.ConnectionString = "<service-bus-connection-string>";
    options.Namespace = "my-namespace";

    options.Retry.MaxDeliveryCount = 10;
    options.DeadLetter.Enabled = true;
});
```

Configuration is **explicit and strongly typed** — no magic strings.
```json
"Messaging": {
  "BootstrapServers": "localhost:9092",
  "GroupId": "my-service",
  "OutboxCollection": "OutboxMessages",
  "DeadLetterCollection": "DeadLetterMessages",
  "InboxCollection": "InboxMessages"
}
````
## ⚡ Dependency Injection
builder.Services.AddFranzAzureEventBus(options =>
{
    options.ConnectionString = configuration["Azure:ServiceBus"];
});
```

This registers:

* `ServiceBusClient`
* `AzureEventBusProducer`
* `AzureEventBusProcessor`
* Mapping & serialization components

⚠️ **Hosting is intentionally NOT included**
See `Franz.Common.Messaging.Azure.Hosting` (planned) for orchestration.

## 🔄 Message Flow

### Producer

1. Domain event published via Franz messaging API
2. Mapped using `Franz.Common.Mapping`
3. Serialized using Franz serializer
4. Sent as `ServiceBusMessage`
5. Headers mapped to `ApplicationProperties`

---

### Consumer

1. Azure Service Bus receives message
2. Message mapped back to Franz envelope
3. Metadata validated
4. Dispatched through **Franz.Mediator**
5. Result:

   * ✅ Complete
   * ⚠️ Retry
   * 🔥 Dead-letter

---

## 📊 Header Mapping

| Franz Header  | Azure Service Bus                               |
| ------------- | ----------------------------------------------- |
| MessageId     | `MessageId`                                     |
| CorrelationId | `CorrelationId`                                 |
| EventType     | `ApplicationProperties["franz-event-type"]`     |
| TenantId      | `ApplicationProperties["franz-tenant-id"]`      |
| SchemaVersion | `ApplicationProperties["franz-schema-version"]` |

All headers are defined in **`AzureEventBusHeaders`**.
---

## 🚀 Extensibility

This package is designed to evolve without breaking contracts:

* Add sessions support
* Integrate Franz Outbox publishing
* Extend DLQ routing strategies
* Support schema evolution & version fallback

Other Azure transports are implemented separately:

* `Franz.Common.Messaging.AzureEventGrid`
* `Franz.Common.Messaging.AzureEventHubs`

## 🧭 Roadmap
* Azure Service Bus sessions
* Outbox publisher integration
* Hosting orchestration package
* Azure Event Grid receiver hosting
* Event Hubs streaming adapter (Kafka-style)


* **Current Version**: **1.7.3**
* Target Framework: **.NET 10**
* Part of the **Franz Framework**

##Licensing

MIT License — see `LICENSE`.

---

## ✅ Changelog

### Version 1.7.0

* Added **Azure Service Bus adapter**
* Franz-native mapping via `Franz.Common.Mapping`
* Mediator-driven consumption pipeline
* Deterministic header & metadata propagation
* Kafka / Rabbit parity for Azure environments

---


