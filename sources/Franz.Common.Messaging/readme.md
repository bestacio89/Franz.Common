# Franz.Common.Messaging

> Broker-neutral message contracts, serialization, factories, header context, and outbox abstractions.

## Overview

The core messaging package defines transport-independent message envelopes, publisher/sender contracts, message factories/builders, serializers, message context, and persistence contracts for inbox/outbox implementations. Broker clients and hosted listeners live in separate transport packages.

## Installation

```bash
dotnet add package Franz.Common.Messaging
```

## Core registrations

The package exposes focused registration extensions:

```csharp
using Franz.Common.Messaging.Extensions;

builder.Services
    .AddMessagingHeaderContext()
    .AddMessagingSerialization()
    .AddMessagingFactories();
```

Transport packages register the core services they require as part of their own composition. Compose only the focused registration extensions exposed by the current package.

## Message contracts

The core model separates:

- `Message`: transport-neutral envelope and metadata.
- `IMessagingSender`: point-to-point transport send contract.
- `IMessagingPublisher`: publish contract.
- `IMessageSerializer`: message body serialization.
- `IMessageFactory` and `IMessageBuilderStrategy`: construction of message envelopes from supported application message types.
- `IMessageStore` and `IInboxStore`: persistence contracts implemented by provider packages.

The package does not create broker connections, declare topics/queues, or start hosted consumers.

## Outbox and inbox boundaries

The outbox publisher service and outbox message listener are separate execution models. Transport-specific hosting packages expose their supported hosted registrations and configure `OutboxOptions`. Storage implementations are supplied by persistence adapters such as MongoDB or Cosmos DB.

The core `AddOutboxPublisher` helper currently registers the configured options instance as `OutboxOptions`, while `OutboxPublisherService` consumes `IOptions<OutboxOptions>`. The callback is therefore not reliably bound to the worker's options object. Do not rely on that helper for configured publisher behavior until the registration is corrected. The Kafka/RabbitMQ `AddOutboxHostedListener` registrations use `services.Configure<OutboxOptions>` and are a separate listener path.

Do not assume that registering a message store alone starts an outbox worker. Register the relevant hosted listener explicitly and configure its polling/retry policy.

## Related packages

- `Franz.Common.Messaging.Kafka`, `Franz.Common.Messaging.RabbitMQ`, and Azure transport packages.
- `Franz.Common.Messaging.Hosting.Kafka` and `Franz.Common.Messaging.Hosting.RabbitMQ`.
- `Franz.Common.Messaging.MultiTenancy`.
- `Franz.Common.MongoDB` and `Franz.Common.AzureCosmosDB` for storage adapters.

## License

MIT. See the repository [LICENSE](../../LICENSE).
