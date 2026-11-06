# Franz.Common.Messaging.Kafka

> Kafka producer, consumer, administration, topic provisioning, manifest validation, and transactional publishing integration.

## Overview

The Kafka adapter implements Franz's broker-neutral messaging contracts using Confluent.Kafka. It supports service-level topic mode and event-based topic mode, configurable provisioning, producer/consumer options, security, dead-letter behavior, and generated messaging-manifest validation.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Kafka
```

## Service-level topic mode

```csharp
using Franz.Common.Messaging.Kafka.Extensions;

builder.Services.AddKafkaMessaging(builder.Configuration);
```

This binds `KafkaMessagingOptions` from `Messaging:Kafka` and registers the Kafka admin, producer, consumer, sender, publisher, transaction, serializer, and topic-initialization infrastructure.

## Event-based topic mode

For one topic per handled integration-event type:

```csharp
using Franz.Common.Messaging.Kafka.Extensions;

builder.Services.AddFranzMediator(new[] { typeof(Program).Assembly });
builder.Services.AddEventBasedKafkaMessaging(builder.Configuration);
```

The parameterless event-based overload derives subscriptions from the generated messaging manifest. An explicit overload accepts `params Type[] eventTypes`; each supplied type must be a concrete `IIntegrationEvent` and simple type names must be unique. Configured subscriptions are checked against the generated manifest. Register Mediator and its manifest contribution before event-based Kafka registration.

## Configuration

The default section is `Messaging:Kafka`:

```json
{
  "Messaging": {
    "Kafka": {
      "BootstrapServers": "localhost:9092",
      "GroupId": "orders",
      "ClientId": "orders-api",
      "ProvisioningMode": "Create",
      "Security": {
        "SecurityProtocol": "Plaintext",
        "AllowInsecureTransport": true
      },
      "Consumer": {
        "EnableAutoCommit": false,
        "EnableAutoOffsetStore": false,
        "ConcurrencyLevel": 1
      },
      "Producer": {
        "Acks": "All",
        "EnableIdempotence": true,
        "CompressionType": "Snappy"
      },
      "Topic": {
        "NumPartitions": 3,
        "ReplicationFactor": 1
      }
    }
  }
}
```

The example uses plaintext only for isolated development. For production, configure TLS/SASL deliberately and keep credentials/certificates in protected deployment configuration. The options validator rejects insecure transport unless it is explicitly allowed.

If `ProvisioningMode` is omitted, development defaults to `Create`; non-development environments default to `Verify`. Supported modes are `Create`, `Verify`, and `Off`. Topic settings can be customized globally and per event topic, including dead-letter topics.

## Hosting

The adapter registers transport services but does not start a hosted listener by itself. Add `Franz.Common.Messaging.Hosting.Kafka` and call `AddKafkaHostedListener(options => ...)` to attach consumption to the generic host. That callback accepts `Action<KafkaMessagingOptions>`, not an IConfiguration parameter.

## Transactions and failure handling

Use the registered `IMessagingTransaction` when a producer transaction is required. Transactional producer settings include idempotence, transaction timeout, and an optional transactional ID prefix. Validate broker transaction settings and topic replication against the target Kafka cluster.

## License

MIT. See the repository [LICENSE](../../LICENSE).
