# Franz.Common.Messaging.AzureEventHubs

> Azure Event Hubs producer and checkpointed processor integration for Franz messages.

## Overview

This package configures Event Hubs clients, a checkpoint store, producer/processor infrastructure, serialization, and message mapping. The Azure hosting package supplies the hosted-service wrapper.

## Installation

```bash
dotnet add package Franz.Common.Messaging.AzureEventHubs
```

## Registration

```csharp
using Franz.Common.Messaging.AzureEventHubs.DependencyInjection;

builder.Services.AddFranzAzureEventHubs(options =>
{
    options.ConnectionString = builder.Configuration["EventHubs:ConnectionString"]!;
    options.EventHubName = "orders";
    options.ConsumerGroup = "$Default";
    options.BlobConnectionString = builder.Configuration["EventHubs:BlobConnectionString"]!;
    options.BlobContainerName = "eventhub-checkpoints";
    options.MaxBatchSize = 100;
});
```

The extension validates required connection, hub, checkpoint-storage, and batch-size settings, then registers the Event Hubs client/processor factories, serializer, mapper, producer, and processor.

## Hosted execution

`AddFranzAzureEventHubs` registers transport infrastructure but does not register the hosted processor. Use `Franz.Common.Messaging.Hosting.Azure` when the host needs the Event Hubs hosted service. Configure the processor's checkpoint storage with an appropriately secured Blob Storage account and container.

## DI lifetime limitation

The current registration creates `AzureEventHubsProcessor` as a singleton and injects `IDispatcher`, which Franz Mediator registers as scoped. With DI scope validation enabled, resolving the processor fails; with validation disabled, it captures a root-scoped dispatcher. Although the processor creates a per-event scope, it resolves the scoped dispatcher from that scope only when `ITenantMessageContextInitializer` is registered. Do not treat this processor registration as production-safe until dispatch always resolves from the per-event scope.

## Tenant-aware processing

When the processor is hosted with a DI scope factory and `ITenantMessageContextInitializer` is registered, each event is dispatched under validated tenant execution context. Register the messaging multi-tenancy package and application-owned tenant store/producer authorizer as required. The processor advances the partition checkpoint only after dispatch succeeds; a dispatch exception leaves the event uncheckpointed for redelivery.

## License

MIT. See the repository [LICENSE](../../LICENSE).
