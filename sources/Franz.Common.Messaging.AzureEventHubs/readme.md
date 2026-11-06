# Franz.Common.Messaging.AzureEventHubs

> Azure Event Hubs producer and checkpointed event-processing integration for Franz messaging.

## Overview

This package integrates Azure Event Hubs with Franz serialization, message mapping, and Mediator dispatch. It uses EventProcessorClient with Blob Storage checkpointing. Event Hubs is an event-streaming service; it is not equivalent to Azure Service Bus queues or topics.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.AzureEventHubs
~~~

## Registration

~~~csharp
using Franz.Common.Messaging.AzureEventHubs.DependencyInjection;

services.AddFranzAzureEventHubs(options =>
{
    options.ConnectionString = configuration["Azure:EventHubs:ConnectionString"]!;
    options.EventHubName = "orders";
    options.ConsumerGroup = "$Default";
    options.BlobConnectionString = configuration["Azure:Storage:ConnectionString"]!;
    options.BlobContainerName = "eventhub-checkpoints";
    options.MaxBatchSize = 100;
});
~~~

AddFranzAzureEventHubs validates options and registers the Event Hubs client/processor factories, serializer, mapper, producer, and processor. It does not register a hosted service. Use Franz.Common.Messaging.Hosting.Azure to bind the processor lifecycle to the generic host.

## Required options

- ConnectionString — Event Hubs namespace connection string.
- EventHubName — target Event Hub.
- ConsumerGroup — consumer group; defaults to $Default.
- BlobConnectionString — required Blob Storage connection string for checkpoint persistence.
- BlobContainerName — checkpoint container name.
- MaxBatchSize — maximum events processed per partition batch.
- StartFromBeginning — controls initial position when no checkpoint exists.

Keep Event Hubs and Storage credentials in secret configuration.

## Processing and checkpoint semantics

AzureEventHubsProcessor deserializes and maps each event, dispatches it through the Franz Mediator notification path, and updates the Event Hubs checkpoint only after dispatch completes successfully. A dispatch failure therefore leaves the checkpoint behind and allows the event to be processed again.

When a DI scope factory and Franz messaging multitenancy integration are available, the processor creates a per-event async scope and establishes the validated tenant context before dispatch. Tenant headers are not authorization; register the tenant store and message authorizer required by Franz.Common.Messaging.MultiTenancy.

Make handlers idempotent. Checkpointing does not create an atomic transaction between Event Hubs and an external database.

## Hosting

Register the processor lifecycle through Franz.Common.Messaging.Hosting.Azure. This package's AddFranzAzureEventHubs method registers the processor but does not start it.

## Related packages

- Franz.Common.Messaging.Hosting.Azure — hosted processor lifecycle and Event Grid endpoint support.
- Franz.Common.Messaging.MultiTenancy — validated tenant context for message delivery.
- Franz.Common.Mediator — in-process notification dispatch.

## License

MIT. See the repository [LICENSE](../../LICENSE).
