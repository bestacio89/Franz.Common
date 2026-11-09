# Franz.Common.Messaging.RabbitMQ

> RabbitMQ sender, publisher, consumer, topology initialization, dead-lettering, replay, and publisher-confirm integration.

## Overview

The RabbitMQ adapter implements Franz's broker-neutral messaging contracts using RabbitMQ.Client. It registers the sender/publisher/consumer infrastructure and initializes the topology required by the generated messaging manifest.

## Installation

```bash
dotnet add package Franz.Common.Messaging.RabbitMQ
```

## Registration

```csharp
using Franz.Common.Messaging.RabbitMQ.Extensions;

builder.Services.AddRabbitMQMessaging(builder.Configuration);
```

The default configuration section is `Messaging:RabbitMQ`. The helper registers the sender, publisher, consumer, options validation, connection/channel infrastructure, and messaging initialization.

## Configuration

```json
{
  "Messaging": {
    "RabbitMQ": {
      "BootStrapServers": "amqps://<user>:<password>@rabbitmq.example.internal:5671/<vhost>",
      "QueueName": "orders",
      "QueueType": "Quorum",
      "QueueDurable": true,
      "MaxConcurrency": 1,
      "ProvisioningMode": "Verify",
      "ReplayStrategy": "ProgressiveFourStep",
      "ManagementApiUrl": "https://rabbitmq.example.internal:15671",
      "PublisherConfirmTimeoutSeconds": 5
    }
  }
}
```

`BootStrapServers` must be an absolute `amqp://` or `amqps://` URI with explicit username/password credentials, or configure host/user/password separately. When TLS is enabled, the URI must use `amqps://`.

## Message envelope mapping

Both `RabbitMQMessagingSender` and `RabbitMQMessagingPublisher` map the Franz message envelope to AMQP basic properties:

- `Message.Id` → `BasicProperties.MessageId` as a GUID in `D` format.
- `Message.CorrelationId` → `BasicProperties.CorrelationId`.
- `Message.CausationHeader` (`x-causation-id`) remains in the AMQP headers table.

On consumption, the listener restores a valid AMQP `MessageId` as `Message.Id`, projects AMQP `CorrelationId` into the Franz correlation header, and retains causation metadata. Other `x-` headers remain filtered as transport infrastructure metadata.

External producers should send a non-empty GUID `MessageId` and a GUID `CorrelationId`. For backward compatibility, missing or invalid message IDs are replaced with a new local ID and marked with `MessagingConstants.TransportGeneratedMessageId` in `Message.Properties`. That local identity is not stable across redelivery and must not be used as an inbox deduplication key. Saga inbox processing rejects these marked deliveries; other consumers may still process them if they do not depend on message-level idempotency. Outbound publication rejects an empty `Message.Id`.

## Topology provisioning

`ProvisioningMode` supports:

- `Create`: create the configured exchanges, queues, dead-letter topology, replay queues, and manifest-derived bindings.
- `Verify`: inspect the expected topology and fail when required exchanges, queues, arguments, or bindings are missing or incompatible.
- `Off`: skip topology provisioning.

Verification of bindings requires the RabbitMQ Management HTTP API because AMQP does not expose passive binding inspection. Configure `ManagementApiUrl` and the management credentials/virtual host when using `Verify`.

The default queue type is quorum. Queue type, durability, TTL, length limits, delivery limits, and replay policy must match the broker topology already provisioned. RabbitMQ rejects redeclarations that conflict with immutable queue arguments.

## Ordering and replay

`MaxConcurrency` defaults to 1. The adapter validates ordering settings against concurrency; configured ordering is preserved only when processing concurrency is 1.

`ReplayStrategy` defaults to `NoReplay`. `ProgressiveFourStep` provisions three retry stages and a dead-letter destination. A configured `FailurePolicy` with retries requires this replay strategy. Dead-letter/retry behavior is not a substitute for idempotent message handlers.

## Hosting

The transport package does not itself attach the listener to the generic host. Add `Franz.Common.Messaging.Hosting.RabbitMQ` and call `AddRabbitMQHostedListener(...)`. The hosting callback currently accepts the shared `MessagingOptions` base type; configure transport-specific RabbitMQ settings through `Messaging:RabbitMQ` and `AddRabbitMQMessaging(configuration)`.

## License

MIT. See the repository [LICENSE](../../LICENSE).
