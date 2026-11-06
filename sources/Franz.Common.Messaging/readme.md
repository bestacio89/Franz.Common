# Franz.Common.Messaging

> Transport-neutral message envelopes, publisher/sender contracts, serializers, builders, storage contracts, and outbox primitives.

## Overview

This package defines the application-facing messaging contracts. Broker-specific connections, topology, consumers, and hosted listener lifecycles belong to transport and hosting packages.

The package exposes explicit registration helpers for shared messaging primitives. Broker configuration and hosted execution belong to the corresponding transport adapters.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging
~~~

## Core contracts

- IMessageSender sends a transport-level Franz Message.
- IMessagingPublisher publishes IIntegrationEvent instances through the configured messaging pipeline.
- IMessagingTransaction represents a transport transaction boundary.
- IMessageBuilder and IMessageBuilderStrategy define message enrichment/building responsibilities.
- IMessageFactory creates message envelopes for commands, queries, integration events, and execution faults.
- IMessageSerializer abstracts message payload serialization.
- IMessageStore and IInboxStore define outbox and inbox persistence contracts.
- IListener defines the asynchronous transport listener contract.

The package also provides Message/MessageKind envelopes, message context contracts, header propagation helpers, and transport capability/provisioning option types.

## Explicit registration helpers

Register only the primitives the application uses:

~~~csharp
using Franz.Common.Messaging.Extensions;

services
    .AddMessagingHeaderContext()
    .AddMessagingSerialization()
    .AddMessagingFactories();
~~~

- AddMessagingHeaderContext registers the messaging header context accessor.
- AddMessagingSerialization registers the default JsonMessageSerializer.
- AddMessagingFactories registers the message factory and built-in command/query/event/fault builder strategies.

Transport packages register their own broker-specific services and options. Do not use a generic MessagingOptions section to configure Kafka or Azure; use each transport's options contract.

## Serialization

JsonMessageSerializer is the default IMessageSerializer implementation. Applications may register another serializer where the transport and stored-message format support it. Serializer compatibility is part of the message contract: producer and consumer must agree on payload representation and type metadata.

## Inbox and outbox contracts

IMessageStore provides pending-message retrieval, sent-state updates, retry updates, and dead-letter movement. IInboxStore records processed message identities so a delivery can be recognized if it is repeated.

The stores are abstractions. Persistence adapters such as Franz.Common.MongoDB and Franz.Common.Messaging.EntityFramework provide implementations. Their tenant isolation and transaction guarantees must be evaluated independently; the generic interfaces do not automatically add tenant filtering.

Outbox processing is not exactly-once execution. A process can complete a business operation and fail before its inbox/outbox state is committed. Design handlers to be idempotent and use a shared transaction or explicit outbox/inbox coordination where the persistence provider supports it.

## Outbox publisher

AddOutboxPublisher(Action<OutboxOptions>) is the current hosted publisher registration helper. The worker requires IMessageStore and IMessagingSender and supports polling interval, batch size, retry count, exponential retry bounds, and dead-letter configuration.

**Registration caveat:** the current helper registers a concrete OutboxOptions instance, while OutboxPublisherService consumes IOptions<OutboxOptions>. The configured instance is therefore not reliably bound to the worker's options wrapper. Do not rely on custom AddOutboxPublisher options until that registration contract is corrected. This is an implementation issue, not a documentation convention.

## Transport packages

- Franz.Common.Messaging.Kafka — Kafka producer/consumer and topic provisioning.
- Franz.Common.Messaging.RabbitMQ — RabbitMQ connections, publishing confirms, topology, and replay.
- Franz.Common.Messaging.AzureEventBus — Azure Service Bus transport.
- Franz.Common.Messaging.AzureEventHubs — Azure Event Hubs transport.
- Franz.Common.Messaging.Hosting.Kafka and Franz.Common.Messaging.Hosting.RabbitMQ — transport-specific hosted execution.
- Franz.Common.Messaging.Hosting.Azure — Azure hosted execution and Event Grid ingress.

## License

MIT. See the repository [LICENSE](../../LICENSE).
