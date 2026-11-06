# Franz.Common.Messaging.Hosting.RabbitMQ

> BackgroundService integration for RabbitMQ consumers and outbox polling.

## Overview

This package attaches the RabbitMQ listener to the .NET generic-host lifecycle. Broker configuration and topology are owned by `Franz.Common.Messaging.RabbitMQ`.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Hosting.RabbitMQ
```

## RabbitMQ listener

Register the transport and then the hosted listener:

```csharp
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.Hosting.RabbitMQ;

builder.Services.AddRabbitMQMessaging(builder.Configuration);

builder.Services.AddRabbitMQHostedListener(_ => { });
```

`AddRabbitMQHostedListener` accepts `Action<MessagingOptions>`. The callback configures the shared base options type; the current RabbitMQ listener consumes `RabbitMQMessagingOptions`, which are bound from `Messaging:RabbitMQ` by `AddRabbitMQMessaging(configuration)`. Configure transport-specific concurrency, queue, replay, and provisioning settings in that RabbitMQ section rather than through the hosting callback.

The hosted service starts the listener in a `BackgroundService`, waits for listener completion, and stops it during host shutdown. When queue provisioning mode is `Verify`, the RabbitMQ initializer requires the Management HTTP API to inspect expected bindings; AMQP passive declarations cannot verify bindings.

## Outbox listener

```csharp
builder.Services.AddOutboxHostedListener(options =>
{
    options.Enabled = true;
    options.BatchSize = 100;
});
```

This registers the generic outbox listener and hosted service. Configure `IMessageStore`, `IInboxStore`, dispatcher, and serializer using the selected storage and messaging packages.

## Tenant processing

Register `Franz.Common.Messaging.MultiTenancy` and enable `RequireTenantScope` when the consumer handles tenant-owned data. The tenant store and producer authorizer remain application responsibilities.

## License

MIT. See the repository [LICENSE](../../LICENSE).
