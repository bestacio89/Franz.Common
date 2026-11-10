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

### Multi-dispatcher coordination

Both the background outbox publisher and the hosted outbox listener use the same ownership-based claim contract:

- A dispatcher atomically claims one eligible message with a unique owner token and a bounded lease.
- Completion, retry updates, and dead-letter transitions require that token and reject expired or superseded ownership.
- Expired leases make abandoned work eligible for recovery after a process crash.
- Dispatchers claim one message at a time. This avoids leasing an entire sequential batch while earlier messages are still being processed.
- MongoDB uses atomic `findOneAndUpdate` claims; EF Core relational Saga storage uses conditional database updates; EF Core Cosmos storage uses document ETag optimistic concurrency.

For relational Saga persistence, the new nullable lease columns are part of the `SagaOutbox` schema. Hosts with an existing relational Saga database must apply an EF Core migration for `SagaOutbox.LeaseOwnerId` and `SagaOutbox.LeaseExpiresAt` before deploying this version; the library does not mutate an existing schema automatically. Cosmos DB and MongoDB persist the additional nullable document fields without a relational migration.

The guarantee is **at-least-once delivery**, not exactly-once side effects. A process can publish successfully and crash before recording completion; a later lease holder may publish again. Consumers must remain idempotent. Configure `OutboxOptions.LeaseDuration` to exceed the expected maximum duration of one dispatch operation. The legacy unowned store methods remain for compatibility, but framework-managed dispatchers exclusively use the ownership-checked methods. Custom `IMessageStore` implementations must implement the atomic claim and ownership-aware methods before they can be used by these dispatchers; the default interface implementations fail closed.

## Related packages

- `Franz.Common.Messaging.Kafka`, `Franz.Common.Messaging.RabbitMQ`, and Azure transport packages.
- `Franz.Common.Messaging.Hosting.Kafka` and `Franz.Common.Messaging.Hosting.RabbitMQ`.
- `Franz.Common.Messaging.MultiTenancy`.
- `Franz.Common.MongoDB` and `Franz.Common.AzureCosmosDB` for storage adapters.

## License

MIT. See the repository [LICENSE](../../LICENSE).
