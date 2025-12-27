# Franz.Common.Messaging.AzureEventHubs

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

* **Current Version**: 1.7.4
* Target Framework: **.NET 10**
* Part of the **Franz Framework**

---

## 📜 License

MIT License — see `LICENSE`.

---

