# Franz.Common.Messaging.AzureEventHubs

> Messaging and distributed integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Messaging.AzureEventHubs` package provides messaging and distributed integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Messaging.AzureEventHubs
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

`Franz.Common.Messaging.AzureEventHubs` is the **Azure Event Hubs streaming transport adapter** for the **Franz Framework messaging stack**.

It provides **high-throughput, partitioned, Kafka-style event streaming** backed by **Azure Event Hubs**, while preserving all Franz architectural guarantees:

* mediator-driven execution
* deterministic metadata propagation
* transport isolation
* cloud-native scalability
* consistent observability

This package is the **streaming counterpart** to:

* `Franz.Common.Messaging.Kafka`
* `Franz.Common.Messaging.AzureEventBus` (Azure Service Bus)
* `Franz.Common.Messaging.AzureEventGrid`

---

## ✨ Features

### 🟦 Azure Event Hubs Streaming

* Native integration with **Azure Event Hubs**
* Partitioned, high-throughput event ingestion
* Consumer-group based scaling
* Azure-managed partition balancing
* Blob-based checkpointing

---

### 🧠 Franz-Native Semantics

* Uses **Franz.Common.Messaging** envelopes
* Dispatches messages through **Franz.Common.Mediator**
* No business logic at the transport layer
* Deterministic propagation of correlation and metadata
* Kafka-parity execution model

---

### 🧩 Explicit Mapping Layer (No AutoMapper)

* Uses **Franz.Common.Mapping**

* Single, authoritative transport boundary:

  * Azure Event Hubs → Franz `Message`

* No reflection magic

* Auditable, version-safe mappings

---

### 🔁 Reliability & Checkpointing

* Uses **Azure Blob Storage** for checkpoints
* Checkpoints updated **only after successful mediator dispatch**
* At-least-once delivery semantics
* Fully Azure-native retry and failure handling

---

### 📊 Observability & Diagnostics

* Integrated with **Franz.Common.Logging**
* Structured logs with Franz conventions
* Partition, offset, and sequence number propagation
* OpenTelemetry compatible (via mediator pipelines)

---

## 📦 Dependencies

This package intentionally depends only on **core Franz building blocks** and Azure SDKs:

```
Franz.Common.Messaging
Franz.Common.Mediator
Franz.Common.Logging
Franz.Common.Errors
Franz.Common.Headers
Franz.Common.Mapping
Franz.Common.Serialization

Azure.Messaging.EventHubs
Azure.Messaging.EventHubs.Processor
Azure.Storage.Blobs
```

❌ No HTTP
❌ No ASP.NET dependencies
❌ No hosting logic
❌ No pull-based consumer APIs

---

## 📂 Project Structure

```
Franz.Common.Messaging.AzureEventHubs/
├── Configuration/
│   └── AzureEventHubsOptions.cs
│
├── Constants/
│   └── AzureEventHubsHeaders.cs
│
├── Consumers/
│   └── AzureEventHubsProcessor.cs
│
├── Producers/
│   └── AzureEventHubsProducer.cs
│
├── Mapping/
│   └── AzureEventHubsMessageMapper.cs
│
├── Serialization/
│   └── AzureEventHubsMessageSerializer.cs
│
├── Infrastructure/
│   ├── EventHubClientFactory.cs
│   └── EventHubProcessorFactory.cs
│
├── DependencyInjection/
│   └── AzureEventHubsServiceCollectionExtensions.cs
│
└── README.md
```

---

## ⚙️ Configuration

Configuration is **explicit and strongly typed** (no magic strings, no IConfiguration coupling):

```csharp
services.AddFranzAzureEventHubs(options =>
{
    options.ConnectionString = "<event-hubs-connection-string>";
    options.EventHubName = "orders-stream";
    options.ConsumerGroup = "$Default";

    options.BlobConnectionString = "<storage-connection-string>";
    options.BlobContainerName = "eventhubs-checkpoints";
});
```

---

## 🔄 Message Flow

### Consumer (Streaming)

1. Azure Event Hubs receives an event
2. `EventProcessorClient` pushes the event to the processor
3. Event mapped to a Franz `Message`
4. Message dispatched through **Franz.Mediator**
5. On success:

   * Blob checkpoint updated
6. On failure:

   * Event retried by Azure Event Hubs

---

### Producer

1. Franz message published
2. Payload serialized using Franz serialization
3. Event sent via `EventHubProducerClient`
4. Partitioning handled by Azure

---

## 📊 Header Mapping

| Franz Header   | Azure Event Hubs Source    |
| -------------- | -------------------------- |
| MessageId      | `EventData.MessageId`      |
| CorrelationId  | `EventData.CorrelationId`  |
| PartitionId    | Processor context          |
| SequenceNumber | `EventData.SequenceNumber` |
| Offset         | `EventData.Offset`         |
| EnqueuedTime   | `EventData.EnqueuedTime`   |

All headers are defined in **`AzureEventHubsHeaders`**.

---

## 🚀 Extensibility

This package is designed to evolve without breaking contracts:

* Custom partitioning strategies
* Schema evolution support
* Outbox integration
* Multi-hub consumption
* Advanced checkpoint policies

Hosting and orchestration are handled separately in a future package:

* `Franz.Common.Messaging.Azure.Hosting`

---

## 📝 Version Information

* **Current Version:** v2.3.0
* Target Framework: **.NET 10**
* Part of the **Franz Framework**

---

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.


---
## 📜 License

MIT License — see `LICENSE`.

---

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
