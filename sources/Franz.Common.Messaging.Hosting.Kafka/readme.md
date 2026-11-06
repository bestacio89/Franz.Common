# Franz.Common.Messaging.Hosting.Kafka

> BackgroundService integration for Kafka message consumption and outbox polling.

## Overview

This package owns the Kafka listener's hosted-service lifecycle. It is separate from `Franz.Common.Messaging.Kafka`, which configures the Kafka producer, consumer, admin client, topics, and transport services.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Hosting.Kafka
```

## Kafka listener

First register the Kafka transport:

```csharp
using Franz.Common.Messaging.Kafka.Extensions;
using Franz.Common.Messaging.Hosting.Kafka;

builder.Services.AddKafkaMessaging(builder.Configuration);

builder.Services.AddKafkaHostedListener(options =>
{
    // Optional overrides for KafkaMessagingOptions.
    options.Consumer.ConcurrencyLevel = 4;
});
```

`AddKafkaHostedListener` accepts an `Action<KafkaMessagingOptions>`, not an `IConfiguration` argument. The transport options are normally bound from `Messaging:Kafka` by `AddKafkaMessaging(configuration)`; the callback applies explicit DI configuration overrides.

The listener runs under `KafkaMessagingHostedService : BackgroundService`. Startup validates required tenant messaging services when `TenantMessagingOptions.RequireTenantScope` is enabled. Message handling occurs in a separate DI scope per delivery.

## Outbox listener

```csharp
builder.Services.AddOutboxHostedListener(options =>
{
    options.Enabled = true;
    options.BatchSize = 100;
});
```

This registers `OutboxMessageListener` and its hosted service. Configure the required `IMessageStore`, `IInboxStore`, dispatcher, and serializer through the application's selected persistence and messaging packages.

## Tenant processing

Register `Franz.Common.Messaging.MultiTenancy` and enable `RequireTenantScope` when the consumer processes tenant-owned data. Provide the tenant store and a producer authorizer based on trusted transport identity.

## License

MIT. See the repository [LICENSE](../../LICENSE).
