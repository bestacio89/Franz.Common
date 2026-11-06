# Franz.Common.Messaging.Hosting.RabbitMQ

> .NET hosted-service integration for RabbitMQ message consumption and outbox processing.

## Overview

This package owns the generic-host lifecycle for RabbitMQ consumption and outbox processing. Broker connections, channels, publisher confirms, topology configuration, and transport behavior belong to Franz.Common.Messaging.RabbitMQ.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.Hosting.RabbitMQ
~~~

## Registration

Register the RabbitMQ transport separately, then register the hosted listeners:

~~~csharp
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.Hosting.RabbitMQ;

services.AddRabbitMQMessaging(configuration);

services.AddRabbitMQHostedListener(options =>
{
    // Configure the generic MessagingOptions used by RabbitMQListener.
});

services.AddOutboxHostedListener(options =>
{
    // Configure the OutboxOptions used by the outbox listener.
});
~~~

AddRabbitMQHostedListener accepts an Action<MessagingOptions>; it does not accept a RabbitMQ connection-string options object. Broker configuration is bound by AddRabbitMQMessaging from the Messaging:RabbitMQ section. AddOutboxHostedListener accepts an Action<OutboxOptions>.

## Execution lifecycle

RabbitMQHostedService keeps the listener attached to the host lifecycle. It optionally provisions the configured queue before listening, remains alive while the consumer is active, and propagates unexpected consumer termination to the host. Shutdown stops the listener before completing the BackgroundService shutdown path.

When the messaging tenant integration is registered, the hosted service validates tenant-scope requirements at startup and the listener execution path establishes a delivery-scoped tenant context. Configure the tenant authorization and store dependencies in Franz.Common.Messaging.MultiTenancy; a tenant header alone is not authorization.

## Operational boundaries

- Register the transport through AddRabbitMQMessaging(configuration).
- Configure broker options in Messaging:RabbitMQ, not through AddRabbitMQHostedListener.
- Use the hosted package only for listener lifecycle and outbox execution.
- Keep message handlers scoped to one delivery and make side effects idempotent.
- Queue provisioning and publisher-confirm behavior are transport concerns; consult the RabbitMQ transport package documentation for the selected provisioning and replay policy.

## License

MIT. See the repository [LICENSE](../../LICENSE).
