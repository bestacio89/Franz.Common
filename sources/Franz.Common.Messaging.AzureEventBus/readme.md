# Franz.Common.Messaging.AzureEventBus

> Azure Service Bus producer and consumer integration for Franz messaging contracts.

## Overview

This package integrates Azure Service Bus with Franz message envelopes and the Mediator dispatcher. It registers both the transport components and an Azure Event Bus hosted consumer. It is not a broker-neutral package and does not make Service Bus delivery exactly-once.

## Installation

~~~bash
dotnet add package Franz.Common.Messaging.AzureEventBus
~~~

## Registration

~~~csharp
using Franz.Common.Messaging.AzureEventBus.DependencyInjection;

services.AddFranzAzureEventBus(options =>
{
    options.ConnectionString = configuration["Azure:ServiceBus"]!;
    options.EntityName = "orders";
    options.MaxConcurrentCalls = 8;
    options.PrefetchCount = 0;
    options.MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5);
});
~~~

AddFranzAzureEventBus accepts an Action<AzureEventBusOptions>, validates the connection string and numeric processor settings, and registers the ServiceBusClient, sender, mapper, processor, consumer, and AzureEventBusHostedService. Supply a valid EntityName for the queue or topic/subscription; the options validator does not verify that entity against the broker.

The producer is registered as IMessagingSender. The package does not register IMessagingPublisher or a generic outbox/inbox store as part of this method.

## Configuration

AzureEventBusOptions includes:

- ConnectionString — required Azure Service Bus connection string.
- EntityName — queue or topic/subscription entity consumed by the processor.
- Namespace — optional diagnostic/naming label.
- DefaultTopic — optional default destination for publishing.
- EnableSessions and SessionIdleTimeout — session-enabled processing settings.
- MaxConcurrentCalls, PrefetchCount, and MaxAutoLockRenewalDuration — processor controls.
- Retry and DeadLetter — transport retry and dead-letter configuration.

Keep connection strings in secret configuration. Configure Service Bus entities with session support when EnableSessions is enabled.

## Delivery lifecycle

The processor disables automatic message completion. Successful processing and failure handling remain under the transport consumer's explicit settlement path. The hosted service starts and stops the consumer with the generic host.

When the messaging tenant integration is registered, hosted startup validates the required tenant dependencies. Tenant context must be established from trusted producer identity and validated tenant data; a tenant header alone is not authorization.

Handlers must be idempotent. Service Bus delivery counts, lock renewal, retries, and dead-letter behavior are transport-level mechanisms and do not make external database side effects atomic.

## Mapping and serialization

The package uses Franz.Common.Mapping for the transport payload mapping profile and a dedicated Azure Event Bus message mapper. The mapping registration is explicit to the package assembly; it still uses the runtime profile-discovery mechanism in Franz.Common.Mapping.

## Hosted composition

For a combined Azure setup that includes Event Hubs and Event Grid ingress, use Franz.Common.Messaging.Hosting.Azure. Do not register the same transport twice through both the standalone and composite registration paths.

## Related packages

- Franz.Common.Messaging — transport-neutral messaging contracts.
- Franz.Common.Messaging.MultiTenancy — validated tenant context for message delivery.
- Franz.Common.Messaging.Hosting.Azure — combined Azure transport and hosted-service composition.
- Franz.Common.Mediator — in-process message dispatch.

## License

MIT. See the repository [LICENSE](../../LICENSE).
